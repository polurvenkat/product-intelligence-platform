using Ardalis.GuardClauses;

namespace ProductIntelligence.Core.Entities;

/// <summary>
/// Strategic goals associated with business domains.
/// Used by AI to align feature prioritization with business objectives.
/// </summary>
public class DomainGoal
{
    public Guid Id { get; private set; }
    public Guid DomainId { get; private set; }
    
    public string GoalDescription { get; private set; }
    public string? TargetQuarter { get; private set; }
    public int Priority { get; private set; }
    
    public DateTime CreatedAt { get; private set; }

    private DomainGoal() { } // For Dapper

    public DomainGoal(
        Guid domainId,
        string goalDescription,
        int priority = 1,
        string? targetQuarter = null)
    {
        Id = Guid.NewGuid();
        DomainId = Guard.Against.Default(domainId, nameof(domainId));
        GoalDescription = Guard.Against.NullOrWhiteSpace(goalDescription, nameof(goalDescription));
        Priority = Guard.Against.NegativeOrZero(priority, nameof(priority));
        TargetQuarter = targetQuarter;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string goalDescription, int priority, string? targetQuarter)
    {
        GoalDescription = Guard.Against.NullOrWhiteSpace(goalDescription, nameof(goalDescription));
        Priority = Guard.Against.NegativeOrZero(priority, nameof(priority));
        TargetQuarter = targetQuarter;
    }
}
