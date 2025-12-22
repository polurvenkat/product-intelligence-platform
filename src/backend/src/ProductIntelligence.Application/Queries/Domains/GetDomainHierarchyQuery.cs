using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Queries.Domains;

/// <summary>
/// Query to retrieve the full domain hierarchy for an organization
/// </summary>
public record GetDomainHierarchyQuery : IRequest<IEnumerable<DomainDto>>
{
    public Guid OrganizationId { get; init; }
}
