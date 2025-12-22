namespace ProductIntelligence.Core.Entities;

public record DomainWithFeatureCount
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? ParentDomainId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Path { get; init; } = string.Empty;
    public Guid? OwnerUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? Metadata { get; init; }
    public long FeatureCount { get; init; }
}
