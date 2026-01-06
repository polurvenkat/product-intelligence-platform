using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Core.Interfaces.Repositories;

public interface IFeedbackRepository
{
    Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feedback>> GetByFeatureIdAsync(Guid featureId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feedback>> GetByRequestIdAsync(Guid featureRequestId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feedback>> GetBySentimentAsync(Guid featureId, Sentiment sentiment, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feedback>> GetAllAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(Feedback entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
