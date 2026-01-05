using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Interfaces;
using ProductIntelligence.Core.Entities;

namespace ProductIntelligence.Application.Services
{
    public class RoadmapManager : IRoadmapManager
    {
        private readonly IRoadmapRepository _repository;
        private readonly IWorkItemManager _workItemManager;

        public RoadmapManager(IRoadmapRepository repository, IWorkItemManager workItemManager)
        {
            _repository = repository;
            _workItemManager = workItemManager;
        }

        public async Task<IEnumerable<RoadmapItemDto>> GetAllRoadmapItemsAsync()
        {
            var items = await _repository.GetAllAsync();
            return items.Select(MapToDto);
        }

        public async Task<RoadmapItemDto?> GetRoadmapItemByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return item != null ? MapToDto(item) : null;
        }

        public async Task<IEnumerable<RoadmapItemDto>> GetRoadmapItemsByYearAsync(int year)
        {
            var items = await _repository.GetByYearAsync(year);
            return items.Select(MapToDto);
        }

        public async Task<RoadmapItemDto> CreateRoadmapItemAsync(CreateRoadmapItemDto dto)
        {
            var item = new RoadmapItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Quarter = dto.Quarter,
                Year = dto.Year,
                Category = dto.Category,
                Status = dto.Status,
                Type = dto.Type,
                Priority = dto.Priority,
                SortOrder = dto.SortOrder,
                ExternalId = dto.ExternalId,
                Progress = dto.Progress,
                Color = dto.Color,
                TargetDate = dto.TargetDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var id = await _repository.CreateAsync(item);
            item.Id = id;
            return MapToDto(item);
        }

        public async Task<bool> UpdateRoadmapItemAsync(UpdateRoadmapItemDto dto)
        {
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing == null) return false;

            existing.Title = dto.Title;
            existing.Description = dto.Description;
            existing.Quarter = dto.Quarter;
            existing.Year = dto.Year;
            existing.Category = dto.Category;
            existing.Status = dto.Status;
            existing.Type = dto.Type;
            existing.Priority = dto.Priority;
            existing.SortOrder = dto.SortOrder;
            existing.ExternalId = dto.ExternalId;
            existing.Progress = dto.Progress;
            existing.Color = dto.Color;
            existing.TargetDate = dto.TargetDate;
            existing.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(existing);
        }

        public async Task<bool> DeleteRoadmapItemAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> UpdateRoadmapSortOrdersAsync(IEnumerable<RoadmapSortOrderDto> items)
        {
            return await _repository.UpdateSortOrdersAsync(items.Select(i => (i.Id, i.SortOrder)));
        }

        public async Task<int?> SyncProgressWithExternalAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null || string.IsNullOrEmpty(item.ExternalId)) return null;

            if (!int.TryParse(item.ExternalId, out var featureId)) return null;

            var details = await _workItemManager.GetFeatureWorkItemsAsync(featureId);
            var total = details.UserStories.Count;
            
            if (total == 0) return item.Progress;

            var completed = details.UserStories.Count(s => s.Status == "Closed" || s.Status == "Done");
            var progress = (int)Math.Round((double)completed / total * 100);

            if (progress != item.Progress)
            {
                item.Progress = progress;
                item.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(item);
            }

            return progress;
        }

        private static RoadmapItemDto MapToDto(RoadmapItem item)
        {
            return new RoadmapItemDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                Quarter = item.Quarter,
                Year = item.Year,
                Category = item.Category,
                Status = item.Status,
                Type = item.Type,
                Priority = item.Priority,
                SortOrder = item.SortOrder,
                ExternalId = item.ExternalId,
                Progress = item.Progress,
                Color = item.Color,
                TargetDate = item.TargetDate
            };
        }
    }
}
