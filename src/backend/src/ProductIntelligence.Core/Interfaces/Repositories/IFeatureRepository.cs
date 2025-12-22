using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Core.Interfaces.Repositories;

public interface IFeatureRepository : IRepository<Feature>
{
    Task<IEnumerable<Feature>> GetByDomainIdAsync(Guid domainId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feature>> GetByStatusAsync(FeatureStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feature>> FindSimilarAsync(float[] embeddingVector, double threshold = 0.85, int limit = 10, CancellationToken cancellationToken = default);
    Task UpdatePriorityAsync(Guid id, decimal score, string reasoning, CancellationToken cancellationToken = default);
}
