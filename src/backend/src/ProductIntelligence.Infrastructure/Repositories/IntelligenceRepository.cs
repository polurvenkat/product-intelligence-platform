using Dapper;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories;

public class IntelligenceRepository : IIntelligenceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public IntelligenceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAnalysisResultAsync(string fileName, string summary, string rawJson, Guid? organizationId = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            INSERT INTO domain_analysis_results (file_name, summary, raw_json_result, organization_id)
            VALUES (@FileName, @Summary, @RawJson::jsonb, @OrganizationId)";
        
        await connection.ExecuteAsync(sql, new { FileName = fileName, Summary = summary, RawJson = rawJson, OrganizationId = organizationId });
    }

    public async Task SaveBusinessContextChunkAsync(string content, int chunkIndex, float[] embedding, string metadataJson, Guid? organizationId = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            INSERT INTO business_context (content, chunk_index, embedding_vector, metadata, organization_id)
            VALUES (@Content, @ChunkIndex, @Embedding::vector, @Metadata::jsonb, @OrganizationId)";
        
        await connection.ExecuteAsync(sql, new { 
            Content = content, 
            ChunkIndex = chunkIndex, 
            Embedding = embedding, 
            Metadata = metadataJson,
            OrganizationId = organizationId
        });
    }

    public async Task<IEnumerable<string>> GetSimilarBusinessContextAsync(float[] embedding, int limit = 5, Guid? organizationId = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            SELECT content 
            FROM business_context 
            WHERE 1=1";
        
        if (organizationId.HasValue)
        {
            sql += " AND organization_id = @OrganizationId";
        }
        else
        {
            sql += " AND organization_id IS NULL"; // For globally shared context if any
        }
        
        sql += " ORDER BY embedding_vector <=> @Embedding::vector LIMIT @Limit";
        
        return await connection.QueryAsync<string>(sql, new { 
            Embedding = embedding, 
            Limit = limit,
            OrganizationId = organizationId
        });
    }

    public async Task<Guid> CreateChatSessionAsync(Guid? userId, Guid? domainId, string title)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            INSERT INTO chat_sessions (user_id, domain_id, title)
            VALUES (@UserId, @DomainId, @Title)
            RETURNING id";
        
        return await connection.ExecuteScalarAsync<Guid>(sql, new { UserId = userId, DomainId = domainId, Title = title });
    }

    public async Task SaveChatMessageAsync(Guid sessionId, string role, string content)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            INSERT INTO chat_messages (session_id, role, content)
            VALUES (@SessionId, @Role, @Content)";
        
        await connection.ExecuteAsync(sql, new { SessionId = sessionId, Role = role, Content = content });
        
        // Also update the session's updated_at
        const string updateSql = "UPDATE chat_sessions SET updated_at = NOW() WHERE id = @SessionId";
        await connection.ExecuteAsync(updateSql, new { SessionId = sessionId });
    }

    public async Task<IEnumerable<ChatTurn>> GetChatHistoryAsync(Guid sessionId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, session_id, role, content, created_at 
            FROM chat_messages 
            WHERE session_id = @SessionId 
            ORDER BY created_at ASC";
        
        return await connection.QueryAsync<ChatTurn>(sql, new { SessionId = sessionId });
    }

    public async Task<IEnumerable<ChatSession>> GetUserChatSessionsAsync(Guid userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        const string sql = @"
            SELECT id, user_id, domain_id, title, created_at, updated_at 
            FROM chat_sessions 
            WHERE user_id = @UserId 
            ORDER BY updated_at DESC";
        
        return await connection.QueryAsync<ChatSession>(sql, new { UserId = userId });
    }
}
