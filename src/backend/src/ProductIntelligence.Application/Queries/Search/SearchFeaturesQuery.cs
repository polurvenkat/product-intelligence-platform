using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Queries.Search;

/// <summary>
/// Query to perform semantic search on features using text input.
/// </summary>
public record SearchFeaturesQuery : IRequest<IEnumerable<FeatureSearchResult>>
{
    public string SearchText { get; init; } = string.Empty;
    public double Threshold { get; init; } = 0.7;
    public int Limit { get; init; } = 20;
}
