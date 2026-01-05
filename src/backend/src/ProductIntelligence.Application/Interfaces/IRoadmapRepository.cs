using System.Collections.Generic;
using System.Threading.Tasks;
using ProductIntelligence.Core.Entities;

namespace ProductIntelligence.Application.Interfaces
{
    public interface IRoadmapRepository
    {
        Task<IEnumerable<RoadmapItem>> GetAllAsync();
        Task<RoadmapItem?> GetByIdAsync(int id);
        Task<IEnumerable<RoadmapItem>> GetByYearAsync(int year);
        Task<int> CreateAsync(RoadmapItem item);
        Task<bool> UpdateAsync(RoadmapItem item);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateSortOrdersAsync(IEnumerable<(int Id, int SortOrder)> items);
    }
}
