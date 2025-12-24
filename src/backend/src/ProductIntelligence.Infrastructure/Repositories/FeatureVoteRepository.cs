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
        const string sql = "SELECT * FROM feature_votes WHERE feature_id = @FeatureId ORDER BY voted_at DESC";
        return await connection.QueryAsync<FeatureVote>(sql, new { FeatureId = featureId });
    }

    public async Task<VoteCount> GetVoteCountAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT 
                COUNT(*) as total_votes,
                SUM(vote_weight) as total_weight,
                SUM(CASE WHEN voter_tier = 'Enterprise' THEN 1 ELSE 0 END) as enterprise_votes,
                SUM(CASE WHEN voter_tier = 'Professional' THEN 1 ELSE 0 END) as professional_votes,
                SUM(CASE WHEN voter_tier = 'Free' THEN 1 ELSE 0 END) as free_votes
            FROM feature_votes 
            WHERE feature_id = @FeatureId";
        
        var result = await connection.QuerySingleOrDefaultAsync<VoteCount>(sql, new { FeatureId = featureId });
        return result ?? new VoteCount();
    }

    public async Task<Guid> AddAsync(FeatureVote entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO feature_votes (id, feature_id, feature_request_id, voter_email,
                voter_company, voter_tier, vote_weight, voted_at)
            VALUES (@Id, @FeatureId, @FeatureRequestId, @VoterEmail,
                @VoterCompany, @VoterTier, @VoteWeight, @VotedAt)
            RETURNING id";
        
        return await connection.ExecuteScalarAsync<Guid>(sql, new {
            entity.Id,
            entity.FeatureId,
            entity.FeatureRequestId,
            entity.VoterEmail,
            entity.VoterCompany,
            entity.VoterTier,
            entity.VoteWeight,
            entity.VotedAt
        });
    }

    public async Task DeleteAsync(Guid featureId, string voterEmail, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM feature_votes WHERE feature_id = @FeatureId AND voter_email = @VoterEmail";
        await connection.ExecuteAsync(sql, new { FeatureId = featureId, VoterEmail = voterEmail });
    }
}
