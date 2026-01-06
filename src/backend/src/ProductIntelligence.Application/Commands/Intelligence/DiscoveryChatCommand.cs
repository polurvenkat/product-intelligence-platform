using MediatR;

namespace ProductIntelligence.Application.Commands.Intelligence;

public record DiscoveryChatCommand : IRequest<DiscoveryChatResponseDto>
{
    public string Message { get; init; } = string.Empty;
    public List<ChatMessageDto> History { get; init; } = new();
    public Guid? SessionId { get; init; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
}

public record ChatMessageDto
{
    public string Role { get; init; } = string.Empty; // "user" or "assistant"
    public string Content { get; init; } = string.Empty;
}

public record DiscoveryChatResponseDto
{
    public string Response { get; init; } = string.Empty;
    public Guid SessionId { get; init; }
    public List<SuggestedActionDto>? SuggestedActions { get; init; }
}

public record SuggestedActionDto
{
    public string Label { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Value { get; init; }
}
