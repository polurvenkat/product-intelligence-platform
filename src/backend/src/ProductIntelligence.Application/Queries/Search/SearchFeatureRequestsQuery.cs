using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Queries.Search;

/// <summary>
/// Query to perform semantic search on feature requests using text input.
/// </summary>
public record SearchFeatureRequestsQuery : IRequest<IEnumerable<FeatureRequestSearchResult>>
{
    public string SearchText { get; init; } = string.Empty;
    public double Threshold { get; init; } = 0.7;
    public int Limit { get; init; } = 20;
}
