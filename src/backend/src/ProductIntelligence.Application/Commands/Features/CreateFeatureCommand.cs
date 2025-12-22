using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Commands.Features;

/// <summary>
/// Command to create a new feature
/// </summary>
public record CreateFeatureCommand : IRequest<FeatureDto>
{
    public Guid DomainId { get; init; }
    public Guid? ParentFeatureId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Priority Priority { get; init; } = Priority.P3;
    public int? EstimatedEffortPoints { get; init; }
    public decimal? BusinessValueScore { get; init; }
    public Guid CreatedBy { get; init; }
    public string? TargetRelease { get; init; }
}
