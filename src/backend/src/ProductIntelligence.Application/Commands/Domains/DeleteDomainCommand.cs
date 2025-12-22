using MediatR;

namespace ProductIntelligence.Application.Commands.Domains;

/// <summary>
/// Command to delete a domain (soft delete or validation that no children exist)
/// </summary>
public record DeleteDomainCommand : IRequest<bool>
{
    public Guid Id { get; init; }
}
