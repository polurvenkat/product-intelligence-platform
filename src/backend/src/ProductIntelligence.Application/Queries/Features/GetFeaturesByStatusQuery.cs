using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Queries.Features;

/// <summary>
/// Query to retrieve features by status
/// </summary>
public record GetFeaturesByStatusQuery : IRequest<IEnumerable<FeatureDto>>
{
    public FeatureStatus Status { get; init; }
}
