using ProductIntelligence.Core.Entities;

namespace ProductIntelligence.Core.Interfaces.Repositories;

public interface IDomainRepository : IRepository<Domain>
{
    Task<IEnumerable<Domain>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Domain>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Domain>> GetHierarchyAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<Domain?> GetByPathAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid domainId, CancellationToken cancellationToken = default);
    Task<int> GetFeatureCountAsync(Guid domainId, CancellationToken cancellationToken = default);
}
