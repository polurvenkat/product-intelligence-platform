using Dapper;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories;

public class FeatureVoteRepository : IFeatureVoteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FeatureVoteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<FeatureVote>> GetByFeatureIdAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<FeatureVote>(
            "SELECT * FROM fn_feature_vote_get_by_feature(@FeatureId)", 
            new { FeatureId = featureId });
    }

    public async Task<VoteCount> GetVoteCountAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var results = await connection.QueryAsync<VoteCount>(
            "SELECT * FROM fn_feature_vote_get_count(@FeatureId)", 
            new { FeatureId = featureId });
        return results.FirstOrDefault() ?? new VoteCount();
    }

    public async Task<Guid> AddAsync(FeatureVote entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(
            @"SELECT fn_feature_vote_add(@Id, @FeatureId, @FeatureRequestId, @VoterEmail, 
                @VoterCompany, @VoterTier, @VoteWeight, @VotedAt)",
            entity);
    }

    public async Task DeleteAsync(Guid featureId, string voterEmail, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "SELECT fn_feature_vote_delete(@FeatureId, @VoterEmail)", 
            new { FeatureId = featureId, VoterEmail = voterEmail });
    }
}
