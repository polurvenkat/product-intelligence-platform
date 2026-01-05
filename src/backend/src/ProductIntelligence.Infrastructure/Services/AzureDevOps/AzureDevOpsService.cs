using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductIntelligence.Application.DTOs.AzureDevOps;
using ProductIntelligence.Application.Interfaces.AzureDevOps;
using ProductIntelligence.Infrastructure.Configuration;

namespace ProductIntelligence.Infrastructure.Services.AzureDevOps;

public class AzureDevOpsService : IAzureDevOpsService
{
    private readonly HttpClient _httpClient;
    private readonly AzureDevOpsOptions _options;

    public AzureDevOpsService(HttpClient httpClient, IOptions<AzureDevOpsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        
        var baseUrl = _options.OrganizationUrl.EndsWith("/") ? _options.OrganizationUrl : _options.OrganizationUrl + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<List<WorkItemDto>> GetChildWorkItemsAsync(int featureId, CancellationToken cancellationToken = default)
    {
        // 1. Get the feature work item to find its children
        var url = $"{_options.Project}/_apis/wit/workitems/{featureId}?$expand=relations&api-version=7.1";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var childIds = new List<int>();
        if (root.TryGetProperty("relations", out var relations))
        {
            foreach (var relation in relations.EnumerateArray())
            {
                if (relation.GetProperty("rel").GetString() == "System.LinkTypes.Hierarchy-Forward")
                {
                    var childUrl = relation.GetProperty("url").GetString();
                    if (childUrl != null)
                    {
                        var idStr = childUrl.Split('/').Last();
                        if (int.TryParse(idStr, out var id))
                        {
                            childIds.Add(id);
                        }
                    }
                }
            }
        }

        if (!childIds.Any()) return new List<WorkItemDto>();

        // 2. Get details for all children
        var workItems = new List<WorkItemDto>();
        foreach (var id in childIds)
        {
            workItems.Add(await GetWorkItemWithHistoryAsync(id, cancellationToken));
        }

        return workItems;
    }

    public async Task<List<WorkItemDto>> GetActiveWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        var wiql = new
        {
            query = "Select [System.Id] From WorkItems Where [System.WorkItemType] IN ('User Story', 'Bug') AND [System.State] IN ('In Progress', 'Code Review', 'QA', 'UAT')"
        };

        var url = $"{_options.Project}/_apis/wit/wiql?api-version=7.1";
        var response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(wiql), Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var workItemsJson = doc.RootElement.GetProperty("workItems");

        var ids = new List<int>();
        foreach (var item in workItemsJson.EnumerateArray())
        {
            ids.Add(item.GetProperty("id").GetInt32());
        }

        var result = new List<WorkItemDto>();
        foreach (var id in ids)
        {
            result.Add(await GetWorkItemWithHistoryAsync(id, cancellationToken));
        }

        return result;
    }

    public async Task<List<FeatureWorkItemsDto>> GetActiveFeaturesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Get all active work items (Stories/Bugs)
        var activeWorkItems = await GetActiveWorkItemsAsync(cancellationToken);
        
        // 2. Find unique parent Feature IDs
        var featureIds = new HashSet<int>();
        foreach (var item in activeWorkItems)
        {
            // We need to fetch the work item with relations to find the parent
            var url = $"{_options.Project}/_apis/wit/workitems/{item.Id}?$expand=relations&api-version=7.1";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) continue;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("relations", out var relations))
            {
                foreach (var relation in relations.EnumerateArray())
                {
                    if (relation.GetProperty("rel").GetString() == "System.LinkTypes.Hierarchy-Reverse")
                    {
                        var parentUrl = relation.GetProperty("url").GetString();
                        if (parentUrl != null)
                        {
                            var idStr = parentUrl.Split('/').Last();
                            if (int.TryParse(idStr, out var id))
                            {
                                featureIds.Add(id);
                            }
                        }
                    }
                }
            }
        }

        // 3. For each feature, get its full hierarchy
        var result = new List<FeatureWorkItemsDto>();
        foreach (var featureId in featureIds)
        {
            // Get feature title
            var featureUrl = $"{_options.Project}/_apis/wit/workitems/{featureId}?api-version=7.1";
            var featureResponse = await _httpClient.GetAsync(featureUrl, cancellationToken);
            if (!featureResponse.IsSuccessStatusCode) continue;

            var featureContent = await featureResponse.Content.ReadAsStringAsync(cancellationToken);
            using var featureDoc = JsonDocument.Parse(featureContent);
            var title = featureDoc.RootElement.GetProperty("fields").GetProperty("System.Title").GetString() ?? "Unknown Feature";

            var children = await GetChildWorkItemsAsync(featureId, cancellationToken);
            
            result.Add(new FeatureWorkItemsDto
            {
                FeatureId = featureId,
                FeatureTitle = title,
                TotalEffort = children.Sum(c => c.Effort),
                TotalDaysSoFar = children.Sum(c => c.TotalDaysSoFar),
                UserStories = children.Where(c => c.Type == "User Story").ToList(),
                Bugs = children.Where(c => c.Type == "Bug").ToList()
            });
        }

        return result;
    }

    public async Task<FeatureWorkItemsDto> GetFeatureDetailsAsync(int featureId, CancellationToken cancellationToken = default)
    {
        // Get feature title
        var featureUrl = $"{_options.Project}/_apis/wit/workitems/{featureId}?api-version=7.1";
        var featureResponse = await _httpClient.GetAsync(featureUrl, cancellationToken);
        featureResponse.EnsureSuccessStatusCode();

        var featureContent = await featureResponse.Content.ReadAsStringAsync(cancellationToken);
        using var featureDoc = JsonDocument.Parse(featureContent);
        var title = featureDoc.RootElement.GetProperty("fields").GetProperty("System.Title").GetString() ?? "Unknown Feature";

        var children = await GetChildWorkItemsAsync(featureId, cancellationToken);
        
        return new FeatureWorkItemsDto
        {
            FeatureId = featureId,
            FeatureTitle = title,
            TotalEffort = children.Sum(c => c.Effort),
            TotalDaysSoFar = children.Sum(c => c.TotalDaysSoFar),
            UserStories = children.Where(c => c.Type == "User Story").ToList(),
            Bugs = children.Where(c => c.Type == "Bug").ToList()
        };
    }

    public async Task<int> CreateFeatureAsync(string title, string description, CancellationToken cancellationToken = default)
    {
        var patchDocument = new[]
        {
            new { op = "add", path = "/fields/System.Title", value = title },
            new { op = "add", path = "/fields/System.Description", value = description ?? "" }
        };

        var url = $"{_options.Project}/_apis/wit/workitems/$Feature?api-version=7.1";
        var content = new StringContent(JsonSerializer.Serialize(patchDocument), Encoding.UTF8, "application/json-patch+json");
        
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseContent);
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public async Task<WorkItemDto> GetWorkItemWithHistoryAsync(int workItemId, CancellationToken cancellationToken = default)
    {
        // Get work item details
        var url = $"{_options.Project}/_apis/wit/workitems/{workItemId}?api-version=7.1";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        var fields = doc.RootElement.GetProperty("fields");

        var workItem = new WorkItemDto
        {
            Id = workItemId,
            Title = fields.GetProperty("System.Title").GetString() ?? "",
            Type = fields.GetProperty("System.WorkItemType").GetString() ?? "",
            Status = fields.GetProperty("System.State").GetString() ?? "",
            AssignedTo = fields.TryGetProperty("System.AssignedTo", out var assignedTo) ? assignedTo.GetProperty("displayName").GetString() ?? "" : "Unassigned",
            CurrentLane = fields.TryGetProperty("System.BoardLane", out var lane) ? lane.GetString() ?? "Default" : "Default",
            BoardColumn = fields.TryGetProperty("System.BoardColumn", out var column) ? column.GetString() ?? "" : "",
            Effort = fields.TryGetProperty("Microsoft.VSTS.Scheduling.Effort", out var effort) ? effort.GetDouble() : 
                     fields.TryGetProperty("Microsoft.VSTS.Scheduling.StoryPoints", out var sp) ? sp.GetDouble() : 0,
            CreatedDate = fields.GetProperty("System.CreatedDate").GetDateTime(),
            ChangedDate = fields.GetProperty("System.ChangedDate").GetDateTime()
        };

        // Get history to calculate lane durations
        var updatesUrl = $"{_options.Project}/_apis/wit/workitems/{workItemId}/updates?api-version=7.1";
        var updatesResponse = await _httpClient.GetAsync(updatesUrl, cancellationToken);
        updatesResponse.EnsureSuccessStatusCode();

        var updatesContent = await updatesResponse.Content.ReadAsStringAsync(cancellationToken);
        using var updatesDoc = JsonDocument.Parse(updatesContent);
        var updates = updatesDoc.RootElement.GetProperty("value");

        var laneDurations = new List<LaneDurationDto>();
        string? currentLane = null;
        DateTime laneEnteredDate = workItem.CreatedDate;
        DateTime lastUpdateRevisedDate = workItem.CreatedDate;

        foreach (var update in updates.EnumerateArray())
        {
            if (update.TryGetProperty("fields", out var uFields) && uFields.TryGetProperty("System.State", out var sUpdate))
            {
                var newState = sUpdate.GetProperty("newValue").GetString();
                
                if (currentLane != null && newState != currentLane)
                {
                    // State changed. The previous state ended when this update occurred.
                    // The time this update occurred is lastUpdateRevisedDate.
                    laneDurations.Add(new LaneDurationDto
                    {
                        LaneName = currentLane,
                        EnteredDate = laneEnteredDate,
                        LeftDate = lastUpdateRevisedDate,
                        Duration = (lastUpdateRevisedDate - laneEnteredDate).TotalDays
                    });
                    laneEnteredDate = lastUpdateRevisedDate;
                }
                currentLane = newState;
            }
            
            // Update lastUpdateRevisedDate for the next iteration
            if (update.TryGetProperty("revisedDate", out var rd) && rd.ValueKind != JsonValueKind.Null)
            {
                lastUpdateRevisedDate = rd.GetDateTime();
            }
            else
            {
                lastUpdateRevisedDate = DateTime.UtcNow;
            }
        }

        // Add the final lane
        if (currentLane != null)
        {
            laneDurations.Add(new LaneDurationDto
            {
                LaneName = currentLane,
                EnteredDate = laneEnteredDate,
                LeftDate = null,
                Duration = (DateTime.UtcNow - laneEnteredDate).TotalDays
            });
        }

        workItem.LaneDurations = laneDurations;

        // Calculate TotalDaysInProgress (sum of durations for specific active states)
        var activeStates = new[] { "In Progress", "Code Review", "QA", "UAT", "Ready for Review" };
        workItem.TotalDaysInProgress = laneDurations
            .Where(l => activeStates.Contains(l.LaneName, StringComparer.OrdinalIgnoreCase))
            .Sum(l => l.Duration);
            
        workItem.TotalDaysSoFar = workItem.TotalDaysInProgress;

        return workItem;
    }
}
