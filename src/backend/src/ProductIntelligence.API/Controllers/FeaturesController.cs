using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.Commands.Features;
using ProductIntelligence.Application.Queries.Features;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.API.Controllers;

/// <summary>
/// Manages features within domains
/// </summary>
[Authorize]
[ApiController]
[Route("api/features")]
[Produces("application/json")]
public class FeaturesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FeaturesController> _logger;

    public FeaturesController(IMediator mediator, ILogger<FeaturesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new feature
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeatureDto>> CreateFeature(
        [FromBody] CreateFeatureCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature created: {FeatureId} - {Title}", result.Id, result.Title);
            return CreatedAtAction(nameof(GetFeature), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Domain not found when creating feature");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create feature");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all features
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FeatureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureDto>>> GetAllFeatures(
        CancellationToken cancellationToken)
    {
        var query = new GetAllFeaturesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a feature by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureDto>> GetFeature(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetFeatureQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Update an existing feature
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeatureDto>> UpdateFeature(
        [FromRoute] Guid id,
        [FromBody] UpdateFeatureCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "ID mismatch between route and body" });
        }

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature updated: {FeatureId}", result.Id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update feature");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a feature
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeature(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteFeatureCommand { Id = id };
            await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature deleted: {FeatureId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all features for a domain
    /// </summary>
    [HttpGet("domain/{domainId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<FeatureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureDto>>> GetFeaturesByDomain(
        [FromRoute] Guid domainId,
        CancellationToken cancellationToken)
    {
        var query = new GetFeaturesByDomainQuery { DomainId = domainId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get features by status
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<FeatureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureDto>>> GetFeaturesByStatus(
        [FromRoute] FeatureStatus status,
        CancellationToken cancellationToken)
    {
        var query = new GetFeaturesByStatusQuery { Status = status };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update feature status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureDto>> UpdateFeatureStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateFeatureStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "ID mismatch between route and body" });
        }

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature status updated: {FeatureId} to {Status}", result.Id, result.Status);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update feature AI priority score
    /// </summary>
    [HttpPatch("{id:guid}/priority")]
    [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureDto>> UpdateFeaturePriority(
        [FromRoute] Guid id,
        [FromBody] UpdateFeaturePriorityCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "ID mismatch between route and body" });
        }

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature priority updated: {FeatureId} to {Score}", result.Id, result.AiPriorityScore);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
