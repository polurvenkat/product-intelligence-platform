using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Commands.Features;

/// <summary>
/// Command to update an existing feature
/// </summary>
public record UpdateFeatureCommand : IRequest<FeatureDto>
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Priority Priority { get; init; }
    public int? EstimatedEffortPoints { get; init; }
    public decimal? BusinessValueScore { get; init; }
    public string? TargetRelease { get; init; }
}
