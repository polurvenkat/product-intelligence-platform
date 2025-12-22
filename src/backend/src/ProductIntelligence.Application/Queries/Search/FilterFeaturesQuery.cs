using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Queries.Search;

/// <summary>
/// Query to filter features with complex criteria.
/// </summary>
public record FilterFeaturesQuery : IRequest<IEnumerable<FeatureDto>>
{
    public Guid? DomainId { get; init; }
    public FeatureStatus? Status { get; init; }
    public Priority? Priority { get; init; }
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public decimal? MinPriorityScore { get; init; }
    public decimal? MaxPriorityScore { get; init; }
    public string? TargetRelease { get; init; }
    public int Limit { get; init; } = 100;
    public int Offset { get; init; } = 0;
}
