using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Queries.Features;

/// <summary>
/// Query to retrieve all features for a domain
/// </summary>
public record GetFeaturesByDomainQuery : IRequest<IEnumerable<FeatureDto>>
{
    public Guid DomainId { get; init; }
}
