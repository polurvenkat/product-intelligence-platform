using MediatR;

namespace ProductIntelligence.Application.Commands.Features;

/// <summary>
/// Command to delete a feature
/// </summary>
public record DeleteFeatureCommand : IRequest<bool>
{
    public Guid Id { get; init; }
}
