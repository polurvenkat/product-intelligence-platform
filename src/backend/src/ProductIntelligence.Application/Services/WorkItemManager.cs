using ProductIntelligence.Application.DTOs.AzureDevOps;
using ProductIntelligence.Application.Interfaces;
using ProductIntelligence.Application.Interfaces.AzureDevOps;

namespace ProductIntelligence.Application.Services;

public class WorkItemManager : IWorkItemManager
{
    private readonly IAzureDevOpsService _azureDevOpsService;

    public WorkItemManager(IAzureDevOpsService azureDevOpsService)
    {
        _azureDevOpsService = azureDevOpsService;
    }

    public async Task<FeatureWorkItemsDto> GetFeatureWorkItemsAsync(int featureId, CancellationToken cancellationToken = default)
    {
        return await _azureDevOpsService.GetFeatureDetailsAsync(featureId, cancellationToken);
    }

    public async Task<List<WorkItemDto>> GetActiveWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _azureDevOpsService.GetActiveWorkItemsAsync(cancellationToken);
    }

    public async Task<List<FeatureWorkItemsDto>> GetActiveFeaturesAsync(CancellationToken cancellationToken = default)
    {
        return await _azureDevOpsService.GetActiveFeaturesAsync(cancellationToken);
    }

    public async Task<int> CreateFeatureAsync(string title, string description, CancellationToken cancellationToken = default)
    {
        return await _azureDevOpsService.CreateFeatureAsync(title, description, cancellationToken);
    }
}
