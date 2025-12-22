using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Commands.Features;

/// <summary>
/// Command to update feature AI priority (typically called by AI agent)
/// </summary>
public record UpdateFeaturePriorityCommand : IRequest<FeatureDto>
{
    public Guid Id { get; init; }
    public decimal PriorityScore { get; init; }
    public string Reasoning { get; init; } = string.Empty;
}
