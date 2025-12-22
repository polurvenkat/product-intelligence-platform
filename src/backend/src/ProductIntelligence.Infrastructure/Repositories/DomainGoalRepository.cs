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
        return await connection.QueryAsync<DomainGoal>(
            "SELECT * FROM fn_domain_goal_get_by_domain(@DomainId)", 
            new { DomainId = domainId });
    }

    public async Task<Guid> AddAsync(DomainGoal entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(
            @"SELECT fn_domain_goal_add(@Id, @DomainId, @GoalDescription, @TargetQuarter, 
                @Priority, @CreatedAt)",
            entity);
    }

    public async Task UpdateAsync(DomainGoal entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "SELECT fn_domain_goal_update(@Id, @GoalDescription, @TargetQuarter, @Priority)",
            entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("SELECT fn_domain_goal_delete(@Id)", new { Id = id });
    }
}
