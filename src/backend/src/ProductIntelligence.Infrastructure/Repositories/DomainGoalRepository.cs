using Dapper;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories;

public interface IDomainGoalRepository
{
    Task<IEnumerable<DomainGoal>> GetByDomainIdAsync(Guid domainId, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(DomainGoal entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(DomainGoal entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class DomainGoalRepository : IDomainGoalRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DomainGoalRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DomainGoal>> GetByDomainIdAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM domain_goals WHERE domain_id = @DomainId ORDER BY priority, target_quarter";
        return await connection.QueryAsync<DomainGoal>(sql, new { DomainId = domainId });
    }

    public async Task<Guid> AddAsync(DomainGoal entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO domain_goals (id, domain_id, goal_description, target_quarter, priority, created_at, updated_at)
            VALUES (@Id, @DomainId, @GoalDescription, @TargetQuarter, @Priority, @CreatedAt, @UpdatedAt)
            RETURNING id";
        
        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task UpdateAsync(DomainGoal entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE domain_goals
            SET goal_description = @GoalDescription,
                target_quarter = @TargetQuarter,
                priority = @Priority,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM domain_goals WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
