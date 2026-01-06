using ProductIntelligence.Core.Entities;

namespace ProductIntelligence.Core.Interfaces.Repositories;

public interface IIntelligenceRepository
{
    Task SaveAnalysisResultAsync(string fileName, string summary, string rawJson, Guid? organizationId = null);
    
    Task SaveBusinessContextChunkAsync(string content, int chunkIndex, float[] embedding, string metadataJson, Guid? organizationId = null);
    
    Task<IEnumerable<string>> GetSimilarBusinessContextAsync(float[] embedding, int limit = 5, Guid? organizationId = null);
    
    // Chat History Persistence (User isolated)
    Task<Guid> CreateChatSessionAsync(Guid? userId, Guid? domainId, string title);
    Task SaveChatMessageAsync(Guid sessionId, string role, string content);
    Task<IEnumerable<ChatTurn>> GetChatHistoryAsync(Guid sessionId);
    Task<IEnumerable<ChatSession>> GetUserChatSessionsAsync(Guid userId);
}
