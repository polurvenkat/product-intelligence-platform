using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Commands.Domains;

/// <summary>
/// Command to update an existing domain
/// </summary>
public record UpdateDomainCommand : IRequest<DomainDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? OwnerUserId { get; init; }
}
