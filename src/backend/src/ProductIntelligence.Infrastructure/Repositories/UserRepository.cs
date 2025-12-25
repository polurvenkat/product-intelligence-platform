using Dapper;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE id = @Id",
            new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE LOWER(email) = LOWER(@Email)",
            new { Email = email });
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            @"SELECT * FROM users 
              WHERE refresh_token = @RefreshToken 
              AND refresh_token_expires_at > @Now",
            new { RefreshToken = refreshToken, Now = DateTime.UtcNow });
    }

    public async Task<User> CreateAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            INSERT INTO users (id, email, name, company, password_hash, tier, created_at, last_login_at, refresh_token, refresh_token_expires_at)
            VALUES (@Id, @Email, @Name, @Company, @PasswordHash, @Tier, @CreatedAt, @LastLoginAt, @RefreshToken, @RefreshTokenExpiresAt)
            RETURNING *";
        
        return await connection.QuerySingleAsync<User>(sql, user);
    }

    public async Task UpdateAsync(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            UPDATE users 
            SET email = @Email,
                name = @Name,
                company = @Company,
                password_hash = @PasswordHash,
                tier = @Tier,
                last_login_at = @LastLoginAt,
                refresh_token = @RefreshToken,
                refresh_token_expires_at = @RefreshTokenExpiresAt
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, user);
    }
}
