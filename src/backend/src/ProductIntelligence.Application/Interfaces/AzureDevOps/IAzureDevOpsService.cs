using ProductIntelligence.Application.DTOs.AzureDevOps;

namespace ProductIntelligence.Application.Interfaces.AzureDevOps;

public interface IAzureDevOpsService
{
    Task<List<WorkItemDto>> GetChildWorkItemsAsync(int featureId, CancellationToken cancellationToken = default);
    Task<WorkItemDto> GetWorkItemWithHistoryAsync(int workItemId, CancellationToken cancellationToken = default);
    Task<List<WorkItemDto>> GetActiveWorkItemsAsync(CancellationToken cancellationToken = default);
    Task<List<FeatureWorkItemsDto>> GetActiveFeaturesAsync(CancellationToken cancellationToken = default);
    Task<FeatureWorkItemsDto> GetFeatureDetailsAsync(int featureId, CancellationToken cancellationToken = default);
    Task<int> CreateFeatureAsync(string title, string description, CancellationToken cancellationToken = default);
}
