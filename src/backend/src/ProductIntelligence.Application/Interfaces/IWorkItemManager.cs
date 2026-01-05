using ProductIntelligence.Application.DTOs.AzureDevOps;

namespace ProductIntelligence.Application.Interfaces;

public interface IWorkItemManager
{
    Task<FeatureWorkItemsDto> GetFeatureWorkItemsAsync(int featureId, CancellationToken cancellationToken = default);
    Task<List<WorkItemDto>> GetActiveWorkItemsAsync(CancellationToken cancellationToken = default);
    Task<List<FeatureWorkItemsDto>> GetActiveFeaturesAsync(CancellationToken cancellationToken = default);
    Task<int> CreateFeatureAsync(string title, string description, CancellationToken cancellationToken = default);
}
