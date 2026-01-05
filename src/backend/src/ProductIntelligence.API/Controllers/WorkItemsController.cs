using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.Interfaces;
using ProductIntelligence.Application.DTOs.AzureDevOps;

namespace ProductIntelligence.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkItemsController : ControllerBase
{
    private readonly IWorkItemManager _workItemManager;

    public WorkItemsController(IWorkItemManager workItemManager)
    {
        _workItemManager = workItemManager;
    }

    [HttpGet("feature/{featureId}")]
    public async Task<ActionResult<FeatureWorkItemsDto>> GetFeatureWorkItems(int featureId, CancellationToken cancellationToken)
    {
        var result = await _workItemManager.GetFeatureWorkItemsAsync(featureId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<WorkItemDto>>> GetActiveWorkItems(CancellationToken cancellationToken)
    {
        var result = await _workItemManager.GetActiveWorkItemsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("features/active")]
    public async Task<ActionResult<List<FeatureWorkItemsDto>>> GetActiveFeatures(CancellationToken cancellationToken)
    {
        var result = await _workItemManager.GetActiveFeaturesAsync(cancellationToken);
        return Ok(result);
    }
}
