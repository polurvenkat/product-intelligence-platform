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
        var results = await connection.QueryAsync<Domain>(
            "SELECT * FROM fn_domain_get_by_id(@Id)", 
            new { Id = id });
        return results.FirstOrDefault();
    }

    public async Task<IEnumerable<Domain>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Domain>("SELECT * FROM fn_domain_get_all()");
    }

    public async Task<IEnumerable<Domain>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Domain>(
            "SELECT * FROM fn_domain_get_by_organization(@OrganizationId)", 
            new { OrganizationId = organizationId });
    }

    public async Task<IEnumerable<Domain>> GetHierarchyAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Domain>(
            "SELECT * FROM fn_domain_get_hierarchy(@OrganizationId)", 
            new { OrganizationId = organizationId });
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
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<Guid>(
            "SELECT fn_domain_add(@Id, @OrganizationId, @ParentDomainId, @Name, @Description, @Path::ltree, @OwnerUserId, @CreatedAt, @UpdatedAt, @Metadata::jsonb)",
            entity);
    }

    public async Task UpdateAsync(Domain entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "SELECT fn_domain_update(@Id, @Name, @Description, @OwnerUserId, @UpdatedAt, @Metadata::jsonb)",
            entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("SELECT fn_domain_delete(@Id)", new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>("SELECT fn_domain_exists(@Id)", new { Id = id });
    }
}
