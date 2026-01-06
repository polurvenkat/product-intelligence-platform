using MediatR;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.Intelligence;

public class ChatQueriesHandler : 
    IRequestHandler<GetChatHistoryQuery, IEnumerable<ChatTurnDto>>,
    IRequestHandler<GetChatSessionsQuery, IEnumerable<ChatSessionDto>>
{
    private readonly IIntelligenceRepository _intelligenceRepository;

    public ChatQueriesHandler(IIntelligenceRepository intelligenceRepository)
    {
        _intelligenceRepository = intelligenceRepository;
    }

    public async Task<IEnumerable<ChatTurnDto>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _intelligenceRepository.GetChatHistoryAsync(request.SessionId);
        return history.Select(h => new ChatTurnDto
        {
            Id = h.Id,
            Role = h.Role,
            Content = h.Content,
            CreatedAt = h.CreatedAt
        });
    }

    public async Task<IEnumerable<ChatSessionDto>> Handle(GetChatSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _intelligenceRepository.GetUserChatSessionsAsync(request.UserId);
        return sessions.Select(s => new ChatSessionDto
        {
            Id = s.Id,
            Title = s.Title,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        });
    }
}
