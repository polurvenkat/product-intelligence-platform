namespace ProductIntelligence.Application.DTOs;

/// <summary>
/// Data transfer object for domain information
/// </summary>
public record DomainDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public int Level { get; init; }
    public int FeatureCount { get; init; }
    public bool HasChildren { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
