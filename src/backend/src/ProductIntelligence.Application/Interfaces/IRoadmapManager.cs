using System.Collections.Generic;
using System.Threading.Tasks;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Interfaces
{
    public interface IRoadmapManager
    {
        Task<IEnumerable<RoadmapItemDto>> GetAllRoadmapItemsAsync();
        Task<RoadmapItemDto?> GetRoadmapItemByIdAsync(int id);
        Task<IEnumerable<RoadmapItemDto>> GetRoadmapItemsByYearAsync(int year);
        Task<RoadmapItemDto> CreateRoadmapItemAsync(CreateRoadmapItemDto dto);
        Task<bool> UpdateRoadmapItemAsync(UpdateRoadmapItemDto dto);
        Task<bool> DeleteRoadmapItemAsync(int id);
        Task<bool> UpdateRoadmapSortOrdersAsync(IEnumerable<RoadmapSortOrderDto> items);
        Task<int?> SyncProgressWithExternalAsync(int id);
    }
}
