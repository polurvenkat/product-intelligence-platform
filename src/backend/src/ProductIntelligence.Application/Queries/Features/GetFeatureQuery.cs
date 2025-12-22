using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Queries.Features;

/// <summary>
/// Query to retrieve a single feature by ID
/// </summary>
public record GetFeatureQuery : IRequest<FeatureDto?>
{
    public Guid Id { get; init; }
}
