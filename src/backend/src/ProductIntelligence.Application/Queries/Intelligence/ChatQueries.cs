using MediatR;

namespace ProductIntelligence.Application.Queries.Intelligence;

public record GetChatHistoryQuery : IRequest<IEnumerable<ChatTurnDto>>
{
    public Guid SessionId { get; init; }
}

public record GetChatSessionsQuery : IRequest<IEnumerable<ChatSessionDto>>
{
    public Guid UserId { get; init; }
}

public record ChatTurnDto
{
    public Guid Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record ChatSessionDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
