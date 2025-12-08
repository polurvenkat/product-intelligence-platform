using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Core.Interfaces.Repositories;

public interface IFeatureRequestRepository : IRepository<FeatureRequest>
{
    Task<IEnumerable<FeatureRequest>> GetByStatusAsync(RequestStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<FeatureRequest>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FeatureRequest>> FindSimilarAsync(float[] embeddingVector, double threshold = 0.85, int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<FeatureRequest>> GetByFeatureIdAsync(Guid featureId, CancellationToken cancellationToken = default);
    Task UpdateEmbeddingAsync(Guid id, float[] embedding, CancellationToken cancellationToken = default);
}
