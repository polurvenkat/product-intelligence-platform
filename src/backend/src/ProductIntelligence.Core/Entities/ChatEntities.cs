namespace ProductIntelligence.Core.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? DomainId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public List<ChatTurn> Messages { get; set; } = new();
}

public class ChatTurn
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty; // user, assistant, system
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
