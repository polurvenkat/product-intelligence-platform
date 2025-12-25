using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Queries.Features;

/// <summary>
/// Query to retrieve all features
/// </summary>
public record GetAllFeaturesQuery : IRequest<IEnumerable<FeatureDto>>
{
}
