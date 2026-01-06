using Dapper;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories;

public class DomainRepository : IDomainRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DomainRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Domain?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM domains WHERE id = @Id";
        return await connection.QuerySingleOrDefaultAsync<Domain>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Domain>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT * FROM domains ORDER BY path";
        return await connection.QueryAsync<Domain>(sql);
    }

    public async Task<IEnumerable<Domain>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        // Since we removed organization_id, just return all domains for now
        return await GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<Domain>> GetHierarchyAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            WITH RECURSIVE domain_tree AS (
                SELECT id, organization_id, parent_domain_id, name, description, path, created_at, updated_at, 0 as level
                FROM domains
                WHERE parent_domain_id IS NULL AND organization_id = @OrganizationId
                
                UNION ALL
                
                SELECT d.id, d.organization_id, d.parent_domain_id, d.name, d.description, d.path, d.created_at, d.updated_at, dt.level + 1
                FROM domains d
                INNER JOIN domain_tree dt ON d.parent_domain_id = dt.id
            )
            SELECT * FROM domain_tree ORDER BY path";
        
        return await connection.QueryAsync<Domain>(sql, new { OrganizationId = organizationId });
    }

    public async Task<IEnumerable<Domain>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM domains WHERE parent_domain_id = @ParentId ORDER BY name";
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Domain>(sql, new { ParentId = parentId });
    }

    public async Task<Domain?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM domains WHERE path = @Path";
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Domain>(sql, new { Path = path });
    }

    public async Task<bool> HasChildrenAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM domains WHERE parent_domain_id = @DomainId)";
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(sql, new { DomainId = domainId });
    }

    public async Task<int> GetFeatureCountAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM features WHERE domain_id = @DomainId";
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(sql, new { DomainId = domainId });
    }

    public async Task<Guid> AddAsync(Domain entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO domains (id, organization_id, parent_domain_id, name, description, path, owner_user_id, created_at, updated_at, metadata)
            VALUES (@Id, @OrganizationId, @ParentDomainId, @Name, @Description, @Path::ltree, @OwnerUserId, @CreatedAt, @UpdatedAt, @Metadata::jsonb)
            RETURNING id";
        
        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task UpdateAsync(Domain entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE domains
            SET name = @Name,
                description = @Description,
                path = @Path::ltree,
                parent_domain_id = @ParentDomainId,
                owner_user_id = @OwnerUserId,
                metadata = @Metadata::jsonb,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        
        // Get the path of the domain we want to delete to find all descendants
        const string getPathSql = "SELECT path FROM domains WHERE id = @Id";
        var path = await connection.ExecuteScalarAsync<string>(getPathSql, new { Id = id });
        
        if (string.IsNullOrEmpty(path)) return;

        // Perform cascading delete in a transaction
        using var transaction = connection.BeginTransaction();
        try
        {
            // 1. Delete feedback for all features in the domain subtree
            const string deleteFeedbackSql = @"
                DELETE FROM feedback 
                WHERE feature_id IN (
                    SELECT f.id FROM features f
                    JOIN domains d ON f.domain_id = d.id
                    WHERE d.path <@ @Path::ltree
                )";
            await connection.ExecuteAsync(deleteFeedbackSql, new { Path = path }, transaction);

            // 2. Delete feature votes for all features in the domain subtree
            const string deleteVotesSql = @"
                DELETE FROM feature_votes 
                WHERE feature_id IN (
                    SELECT f.id FROM features f
                    JOIN domains d ON f.domain_id = d.id
                    WHERE d.path <@ @Path::ltree
                )";
            await connection.ExecuteAsync(deleteVotesSql, new { Path = path }, transaction);

            // 3. Delete domain goals in the subtree
            const string deleteGoalsSql = @"
                DELETE FROM domain_goals 
                WHERE domain_id IN (
                    SELECT id FROM domains WHERE path <@ @Path::ltree
                )";
            await connection.ExecuteAsync(deleteGoalsSql, new { Path = path }, transaction);

            // 4. Delete features in the domain subtree
            const string deleteFeaturesSql = @"
                DELETE FROM features 
                WHERE domain_id IN (
                    SELECT id FROM domains WHERE path <@ @Path::ltree
                )";
            await connection.ExecuteAsync(deleteFeaturesSql, new { Path = path }, transaction);

            // 5. Finally delete the domains themselves (this will include the parent and all child domains)
            const string deleteDomainsSql = "DELETE FROM domains WHERE path <@ @Path::ltree";
            await connection.ExecuteAsync(deleteDomainsSql, new { Path = path }, transaction);

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM domains WHERE id = @Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }
}
