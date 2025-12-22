using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.Domains;

/// <summary>
/// Handler for retrieving the full domain hierarchy for an organization
/// </summary>
public class GetDomainHierarchyQueryHandler : IRequestHandler<GetDomainHierarchyQuery, IEnumerable<DomainDto>>
{
    private readonly IDomainRepository _domainRepository;

    public GetDomainHierarchyQueryHandler(IDomainRepository domainRepository)
    {
        _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
    }

    public async Task<IEnumerable<DomainDto>> Handle(GetDomainHierarchyQuery request, CancellationToken cancellationToken)
    {
        var domains = await _domainRepository.GetHierarchyAsync(request.OrganizationId, cancellationToken);
        
        // Get metadata for all domains in parallel
        var domainList = domains.ToList();
        var featureCounts = await Task.WhenAll(
            domainList.Select(d => _domainRepository.GetFeatureCountAsync(d.Id, cancellationToken)));
        var hasChildrenFlags = await Task.WhenAll(
            domainList.Select(d => _domainRepository.HasChildrenAsync(d.Id, cancellationToken)));

        var result = domainList.Select((domain, index) => new DomainDto
        {
            Id = domain.Id,
            OrganizationId = domain.OrganizationId,
            Name = domain.Name,
            Description = domain.Description,
            Path = domain.Path,
            ParentId = domain.ParentDomainId,
            FeatureCount = featureCounts[index],
            HasChildren = hasChildrenFlags[index],
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        }).ToList();

        return result;
    }
}
