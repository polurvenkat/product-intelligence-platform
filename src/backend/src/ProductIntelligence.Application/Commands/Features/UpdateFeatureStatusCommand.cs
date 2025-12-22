using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Commands.Features;

/// <summary>
/// Command to update feature status
/// </summary>
public record UpdateFeatureStatusCommand : IRequest<FeatureDto>
{
    public Guid Id { get; init; }
    public FeatureStatus Status { get; init; }
}
