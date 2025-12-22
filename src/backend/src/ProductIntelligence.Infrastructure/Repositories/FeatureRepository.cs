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
        var results = await connection.QueryAsync<Feature>(
            "SELECT * FROM fn_feature_get_by_id(@Id)", 
            new { Id = id });
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Feature>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Feature>("SELECT * FROM fn_feature_get_all()");
    }

    public async Task<IEnumerable<Feature>> GetByDomainIdAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Feature>(
            "SELECT * FROM fn_feature_get_by_domain(@DomainId)", 
            new { DomainId = domainId });
    }

    public async Task<IEnumerable<Feature>> GetByStatusAsync(FeatureStatus status, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Feature>(
            "SELECT * FROM fn_feature_get_by_status(@Status)", 
            new { Status = status.ToString() });
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
        return await connection.QueryAsync<FeatureWithVoteCount>(
            "SELECT * FROM fn_feature_get_with_vote_count(@DomainId)", 
            new { DomainId = domainId });
    }

    public async Task UpdatePriorityAsync(Guid id, decimal score, string reasoning, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "SELECT fn_feature_update_priority(@Id, @Score, @Reasoning, @UpdatedAt)",
            new 
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
        return await connection.ExecuteScalarAsync<Guid>(
            @"SELECT fn_feature_add(@Id, @DomainId, @ParentFeatureId, @Title, @Description, @Status, @Priority, 
                @AiPriorityScore, @AiPriorityReasoning, @EstimatedEffortPoints, @BusinessValueScore, 
                @CreatedBy, @CreatedAt, @UpdatedAt, @TargetRelease, @Metadata::jsonb)",
            entity);
    }

    public async Task UpdateAsync(Feature entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            @"SELECT fn_feature_update(@Id, @Title, @Description, @Status, @Priority, 
                @EstimatedEffortPoints, @BusinessValueScore, @UpdatedAt, @TargetRelease, @Metadata::jsonb)",
            entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("SELECT fn_feature_delete(@Id)", new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>("SELECT fn_feature_exists(@Id)", new { Id = id });
    }
}

public record FeatureWithVoteCount
{
    public Guid Id { get; init; }
    public Guid DomainId { get; init; }
    public Guid? ParentFeatureId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public decimal AiPriorityScore { get; init; }
    public string? AiPriorityReasoning { get; init; }
    public int? EstimatedEffortPoints { get; init; }
    public decimal? BusinessValueScore { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? TargetRelease { get; init; }
    public string? Metadata { get; init; }
    public long VoteCount { get; init; }
    public long WeightedVoteCount { get; init; }
}
