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
        const string sql = @"
            SELECT id, organization_id, parent_domain_id, name, description, path, 
                   owner_user_id, created_at, updated_at, metadata
            FROM domains
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Domain>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Domain>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, organization_id, parent_domain_id, name, description, path, 
                   owner_user_id, created_at, updated_at, metadata
            FROM domains
            ORDER BY path";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Domain>(sql);
    }

    public async Task<IEnumerable<Domain>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, organization_id, parent_domain_id, name, description, path, 
                   owner_user_id, created_at, updated_at, metadata
            FROM domains
            WHERE organization_id = @OrganizationId
            ORDER BY path";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Domain>(sql, new { OrganizationId = organizationId });
    }

    public async Task<IEnumerable<Domain>> GetHierarchyAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT d.id, d.organization_id, d.parent_domain_id, d.name, d.description, d.path, 
                   d.owner_user_id, d.created_at, d.updated_at, d.metadata,
                   COUNT(f.id) as feature_count
            FROM domains d
            LEFT JOIN features f ON f.domain_id = d.id
            WHERE d.organization_id = @OrganizationId
            GROUP BY d.id
            ORDER BY d.path";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Domain>(sql, new { OrganizationId = organizationId });
    }

    public async Task<IEnumerable<Domain>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, organization_id, parent_domain_id, name, description, path, 
                   owner_user_id, created_at, updated_at, metadata
            FROM domains
            WHERE parent_domain_id = @ParentId
            ORDER BY name";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Domain>(sql, new { ParentId = parentId });
    }

    public async Task<Domain?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, organization_id, parent_domain_id, name, description, path, 
                   owner_user_id, created_at, updated_at, metadata
            FROM domains
            WHERE path = @Path::ltree";

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
        const string sql = @"
            INSERT INTO domains (id, organization_id, parent_domain_id, name, description, path, 
                                owner_user_id, created_at, updated_at, metadata)
            VALUES (@Id, @OrganizationId, @ParentDomainId, @Name, @Description, @Path::ltree, 
                    @OwnerUserId, @CreatedAt, @UpdatedAt, @Metadata::jsonb)
            RETURNING id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(sql, entity);
    }

    public async Task UpdateAsync(Domain entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE domains
            SET name = @Name,
                description = @Description,
                owner_user_id = @OwnerUserId,
                updated_at = @UpdatedAt,
                metadata = @Metadata::jsonb
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM domains WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM domains WHERE id = @Id)";

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }
}
