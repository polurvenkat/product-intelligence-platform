using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.DTOs.AzureDevOps;
using ProductIntelligence.Application.Interfaces;

namespace ProductIntelligence.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoadmapsController : ControllerBase
    {
        private readonly IRoadmapManager _manager;
        private readonly IWorkItemManager _workItemManager;

        public RoadmapsController(IRoadmapManager manager, IWorkItemManager workItemManager)
        {
            _manager = manager;
            _workItemManager = workItemManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoadmapItemDto>>> GetAll()
        {
            var items = await _manager.GetAllRoadmapItemsAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoadmapItemDto>> GetById(int id)
        {
            var item = await _manager.GetRoadmapItemByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("year/{year}")]
        public async Task<ActionResult<IEnumerable<RoadmapItemDto>>> GetByYear(int year)
        {
            var items = await _manager.GetRoadmapItemsByYearAsync(year);
            return Ok(items);
        }

        [HttpPost]
        public async Task<ActionResult<RoadmapItemDto>> Create(CreateRoadmapItemDto dto)
        {
            var item = await _manager.CreateRoadmapItemAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateRoadmapItemDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch");
            
            var result = await _manager.UpdateRoadmapItemAsync(dto);
            if (!result) return NotFound();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _manager.DeleteRoadmapItemAsync(id);
            if (!result) return NotFound();
            
            return NoContent();
        }

        [HttpPost("reorder")]
        public async Task<IActionResult> Reorder(IEnumerable<RoadmapSortOrderDto> items)
        {
            var result = await _manager.UpdateRoadmapSortOrdersAsync(items);
            if (!result) return BadRequest("Failed to update sort orders");
            return NoContent();
        }

        [HttpGet("{id}/external-details")]
        public async Task<ActionResult<FeatureWorkItemsDto>> GetExternalDetails(int id)
        {
            var item = await _manager.GetRoadmapItemByIdAsync(id);
            if (item == null) return NotFound();
            if (string.IsNullOrEmpty(item.ExternalId)) return BadRequest("No external ID linked");

            if (!int.TryParse(item.ExternalId, out var featureId))
            {
                return BadRequest("Invalid external ID format");
            }

            // Sync progress while we're at it
            await _manager.SyncProgressWithExternalAsync(id);

            var details = await _workItemManager.GetFeatureWorkItemsAsync(featureId);
            return Ok(details);
        }

        [HttpPost("{id}/sync")]
        public async Task<ActionResult<int>> SyncProgress(int id)
        {
            var progress = await _manager.SyncProgressWithExternalAsync(id);
            if (progress == null) return NotFound();
            return Ok(progress);
        }

        [HttpPost("sync-all")]
        public async Task<IActionResult> SyncAllProgress()
        {
            var items = await _manager.GetAllRoadmapItemsAsync();
            var linkedItems = items.Where(i => !string.IsNullOrEmpty(i.ExternalId));
            
            foreach (var item in linkedItems)
            {
                await _manager.SyncProgressWithExternalAsync(item.Id);
            }
            
            return NoContent();
        }

        [HttpPost("{id}/create-external")]
        public async Task<ActionResult<RoadmapItemDto>> CreateExternalFeature(int id)
        {
            var item = await _manager.GetRoadmapItemByIdAsync(id);
            if (item == null) return NotFound();
            if (!string.IsNullOrEmpty(item.ExternalId)) return BadRequest("Already linked to an external feature");

            var featureId = await _workItemManager.CreateFeatureAsync(item.Title, item.Description ?? "");
            
            // Update the roadmap item with the new external ID
            var updateDto = new UpdateRoadmapItemDto
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
                ExternalId = featureId.ToString(),
                Progress = item.Progress,
                Color = item.Color,
                TargetDate = item.TargetDate
            };

            await _manager.UpdateRoadmapItemAsync(updateDto);
            
            return Ok(await _manager.GetRoadmapItemByIdAsync(id));
        }
    }
}
