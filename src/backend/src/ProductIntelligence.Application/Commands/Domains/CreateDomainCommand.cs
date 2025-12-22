using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Commands.Domains;

public record CreateDomainCommand : IRequest<DomainDto>
{
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentDomainId { get; init; }
    public Guid? OwnerUserId { get; init; }
}
