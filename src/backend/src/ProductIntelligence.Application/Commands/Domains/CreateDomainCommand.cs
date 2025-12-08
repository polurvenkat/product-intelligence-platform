using MediatR;
using ProductIntelligence.Core.Entities;

namespace ProductIntelligence.Application.Commands.Domains;

public record CreateDomainCommand : IRequest<DomainDto>
{
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentDomainId { get; init; }
    public Guid? OwnerUserId { get; init; }
}

public record DomainDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? ParentDomainId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Path { get; init; } = string.Empty;
    public int Level { get; init; }
    public int FeatureCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
