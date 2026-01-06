using Dapper;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories;

public class FeatureRepository : IFeatureRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FeatureRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Feature?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, domain_id, title, description, status, priority,
                   ai_priority_score, ai_priority_reasoning, estimated_effort_points,
                   business_value_score, created_at, updated_at
            FROM features WHERE id = @Id";
        return await connection.QuerySingleOrDefaultAsync<Feature>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Feature>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, domain_id, title, description, status, priority,
                   ai_priority_score, ai_priority_reasoning, estimated_effort_points,
                   business_value_score, created_at, updated_at
            FROM features ORDER BY created_at DESC";
        return await connection.QueryAsync<Feature>(sql);
    }

    public async Task<IEnumerable<Feature>> GetByDomainIdAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, domain_id, title, description, status, priority,
                   ai_priority_score, ai_priority_reasoning, estimated_effort_points,
                   business_value_score, created_at, updated_at
            FROM features WHERE domain_id = @DomainId ORDER BY created_at DESC";
        return await connection.QueryAsync<Feature>(sql, new { DomainId = domainId });
    }

    public async Task<IEnumerable<Feature>> GetByStatusAsync(FeatureStatus status, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT id, domain_id, title, description, status, priority,
                   ai_priority_score, ai_priority_reasoning, estimated_effort_points,
                   business_value_score, created_at, updated_at
            FROM features WHERE status = @Status ORDER BY ai_priority_score DESC NULLS LAST, created_at DESC";
        return await connection.QueryAsync<Feature>(sql, new { Status = status.ToString() });
    }

    public async Task<IEnumerable<Feature>> FindSimilarAsync(
        float[] embeddingVector, 
        double threshold = 0.85, 
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Feature>(
            "SELECT * FROM fn_feature_find_similar(@EmbeddingVector::vector, @Threshold, @Limit)",
            new 
            { 
                EmbeddingVector = embeddingVector, 
                Threshold = threshold,
                Limit = limit
            });
    }

    public async Task<IEnumerable<FeatureWithVoteCount>> GetWithVoteCountAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT f.*, 
                   COUNT(fv.id) as vote_count,
                   COALESCE(SUM(fv.weight), 0) as weighted_vote_count
            FROM features f
            LEFT JOIN feature_votes fv ON f.id = fv.feature_id
            WHERE f.domain_id = @DomainId
            GROUP BY f.id
            ORDER BY weighted_vote_count DESC, f.created_at DESC";
        return await connection.QueryAsync<FeatureWithVoteCount>(sql, new { DomainId = domainId });
    }

    public async Task UpdatePriorityAsync(Guid id, decimal score, string reasoning, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE features
            SET ai_priority_score = @Score,
                ai_priority_reasoning = @Reasoning,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            Score = score, 
            Reasoning = reasoning,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<Guid> AddAsync(Feature entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO features (id, domain_id, title, description, status, priority, 
                estimated_effort_points, business_value_score, ai_priority_score, 
                ai_priority_reasoning, target_release, created_by, created_at, updated_at)
            VALUES (@Id, @DomainId, @Title, @Description, @Status, @Priority,
                @EstimatedEffortPoints, @BusinessValueScore, @AiPriorityScore,
                @AiPriorityReasoning, @TargetRelease, @CreatedBy, @CreatedAt, @UpdatedAt)
            RETURNING id";
        
        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task UpdateAsync(Feature entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE features
            SET title = @Title,
                description = @Description,
                status = @Status,
                priority = @Priority,
                estimated_effort_points = @EstimatedEffortPoints,
                business_value_score = @BusinessValueScore,
                target_release = @TargetRelease,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM features WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IEnumerable<FeatureWithVoteCount>> GetTopFeaturesAsync(int limit = 5, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT f.id, f.domain_id, f.title, f.description, f.status, f.priority,
                   f.ai_priority_score, f.ai_priority_reasoning, f.estimated_effort_points,
                   f.business_value_score, f.created_at, f.updated_at,
                   COUNT(fv.id) as vote_count,
                   COALESCE(SUM(fv.vote_weight), 0) as weighted_vote_count
            FROM features f
            LEFT JOIN feature_votes fv ON f.id = fv.feature_id
            GROUP BY f.id
            ORDER BY f.ai_priority_score DESC NULLS LAST, weighted_vote_count DESC, f.created_at DESC
            LIMIT @Limit";
        return await connection.QueryAsync<FeatureWithVoteCount>(sql, new { Limit = limit });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM features WHERE id = @Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
