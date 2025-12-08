using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Core.Interfaces.Repositories;

public interface IFeatureRepository : IRepository<Feature>
{
    Task<IEnumerable<Feature>> GetByDomainIdAsync(Guid domainId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feature>> GetByStatusAsync(FeatureStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feature>> GetByPriorityAsync(Priority priority, CancellationToken cancellationToken = default);
    Task<int> GetVoteCountAsync(Guid featureId, CancellationToken cancellationToken = default);
    Task<int> GetWeightedVoteCountAsync(Guid featureId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feature>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
