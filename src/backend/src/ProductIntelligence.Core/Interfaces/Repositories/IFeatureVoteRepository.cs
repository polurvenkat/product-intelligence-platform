using ProductIntelligence.Core.Entities;

namespace ProductIntelligence.Core.Interfaces.Repositories;

public interface IFeatureVoteRepository
{
    Task<IEnumerable<FeatureVote>> GetByFeatureIdAsync(Guid featureId, CancellationToken cancellationToken = default);
    Task<VoteCount> GetVoteCountAsync(Guid featureId, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(FeatureVote entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid featureId, string voterEmail, CancellationToken cancellationToken = default);
}

public record VoteCount
{
    public long Count { get; init; }
    public long WeightedCount { get; init; }
}
