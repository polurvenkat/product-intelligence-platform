using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ProductIntelligence.Application.Interfaces;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Infrastructure.Data;

namespace ProductIntelligence.Infrastructure.Repositories
{
    public class RoadmapRepository : IRoadmapRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RoadmapRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<RoadmapItem>> GetAllAsync()
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            const string sql = @"
                SELECT id, title, description, quarter, year, category, status, type, priority, sort_order as SortOrder, external_id as ExternalId, progress, color, target_date as TargetDate, created_at as CreatedAt, updated_at as UpdatedAt
                FROM roadmap_items 
                ORDER BY year DESC, quarter DESC, sort_order ASC, created_at DESC";
            return await connection.QueryAsync<RoadmapItem>(sql);
        }

        public async Task<RoadmapItem?> GetByIdAsync(int id)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            const string sql = @"
                SELECT id, title, description, quarter, year, category, status, type, priority, sort_order as SortOrder, external_id as ExternalId, progress, color, target_date as TargetDate, created_at as CreatedAt, updated_at as UpdatedAt
                FROM roadmap_items 
                WHERE id = @Id";
            return await connection.QuerySingleOrDefaultAsync<RoadmapItem>(sql, new { Id = id });
        }

        public async Task<IEnumerable<RoadmapItem>> GetByYearAsync(int year)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            const string sql = @"
                SELECT id, title, description, quarter, year, category, status, type, priority, sort_order as SortOrder, external_id as ExternalId, progress, color, target_date as TargetDate, created_at as CreatedAt, updated_at as UpdatedAt
                FROM roadmap_items 
                WHERE year = @Year
                ORDER BY quarter DESC, sort_order ASC, created_at DESC";
            return await connection.QueryAsync<RoadmapItem>(sql, new { Year = year });
        }

        public async Task<int> CreateAsync(RoadmapItem item)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            const string sql = @"
                INSERT INTO roadmap_items (title, description, quarter, year, category, status, type, priority, sort_order, external_id, progress, color, target_date, created_at, updated_at)
                VALUES (@Title, @Description, @Quarter, @Year, @Category, @Status, @Type, @Priority, @SortOrder, @ExternalId, @Progress, @Color, @TargetDate, @CreatedAt, @UpdatedAt)
                RETURNING id";
            
            return await connection.QuerySingleAsync<int>(sql, new {
                item.Title,
                item.Description,
                item.Quarter,
                item.Year,
                item.Category,
                item.Status,
                item.Type,
                item.Priority,
                item.SortOrder,
                item.ExternalId,
                item.Progress,
                item.Color,
                TargetDate = item.TargetDate?.ToUniversalTime(),
                CreatedAt = item.CreatedAt.ToUniversalTime(),
                UpdatedAt = item.UpdatedAt.ToUniversalTime()
            });
        }

        public async Task<bool> UpdateAsync(RoadmapItem item)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            const string sql = @"
                UPDATE roadmap_items 
                SET title = @Title, 
                    description = @Description, 
                    quarter = @Quarter, 
                    year = @Year, 
                    category = @Category, 
                    status = @Status, 
                    type = @Type, 
                    priority = @Priority,
                    sort_order = @SortOrder,
                    external_id = @ExternalId,
                    progress = @Progress, 
                    color = @Color, 
                    target_date = @TargetDate, 
                    updated_at = @UpdatedAt
                WHERE id = @Id";
            
            var rowsAffected = await connection.ExecuteAsync(sql, new {
                item.Id,
                item.Title,
                item.Description,
                item.Quarter,
                item.Year,
                item.Category,
                item.Status,
                item.Type,
                item.Priority,
                item.SortOrder,
                item.ExternalId,
                item.Progress,
                item.Color,
                TargetDate = item.TargetDate?.ToUniversalTime(),
                UpdatedAt = item.UpdatedAt.ToUniversalTime()
            });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            const string sql = "DELETE FROM roadmap_items WHERE id = @Id";
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateSortOrdersAsync(IEnumerable<(int Id, int SortOrder)> items)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                const string sql = "UPDATE roadmap_items SET sort_order = @SortOrder, updated_at = @UpdatedAt WHERE id = @Id";
                foreach (var item in items)
                {
                    await connection.ExecuteAsync(sql, new { Id = item.Id, SortOrder = item.SortOrder, UpdatedAt = DateTime.UtcNow }, transaction);
                }
                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }
    }
}
