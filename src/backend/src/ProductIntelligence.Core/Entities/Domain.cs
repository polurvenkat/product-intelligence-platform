using Ardalis.GuardClauses;
using ProductIntelligence.Core.ValueObjects;

namespace ProductIntelligence.Core.Entities;

/// <summary>
/// Represents a business domain or sub-domain in the hierarchical structure.
/// Supports unlimited nesting via parent-child relationships.
/// </summary>
public class Domain
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? ParentDomainId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    
    /// <summary>
    /// Hierarchical path using ltree format (e.g., 'customer_mgmt.user_auth')
    /// </summary>
    public string Path { get; private set; }
    
    public Guid? OwnerUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Dictionary<string, object>? Metadata { get; private set; }

    // Navigation properties (not mapped to DB, loaded separately)
    public List<Feature> Features { get; set; } = new();
    public List<DomainGoal> Goals { get; set; } = new();

    private Domain() { } // For Dapper

    public Domain(
        Guid organizationId,
        string name,
        string? description = null,
        Guid? parentDomainId = null,
        string? parentPath = null,
        Guid? ownerUserId = null,
        Dictionary<string, object>? metadata = null)
    {
        Id = Guid.NewGuid();
        OrganizationId = Guard.Against.Default(organizationId, nameof(organizationId));
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = description;
        ParentDomainId = parentDomainId;
        OwnerUserId = ownerUserId;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Build ltree path
        var sanitizedName = SanitizeForPath(name);
        Path = parentPath != null 
            ? $"{parentPath}.{sanitizedName}" 
            : sanitizedName;
    }

    public void Update(string name, string? description, Guid? ownerUserId)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = description;
        OwnerUserId = ownerUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMetadata(Dictionary<string, object> metadata)
    {
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string SanitizeForPath(string input)
    {
        return input.ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("-", "_")
            .Trim('_');
    }

    public int GetLevel() => Path.Split('.').Length;
    
    public bool IsRoot() => ParentDomainId == null;
}
