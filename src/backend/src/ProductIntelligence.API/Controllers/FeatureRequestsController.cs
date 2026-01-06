using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.Commands.FeatureRequests;
using ProductIntelligence.Application.Queries.FeatureRequests;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.API.Controllers;

/// <summary>
/// Manages feature requests and their lifecycle.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/feature-requests")]
public class FeatureRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FeatureRequestsController> _logger;

    public FeatureRequestsController(IMediator mediator, ILogger<FeatureRequestsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new feature request.
    /// </summary>
    /// <param name="command">Feature request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created feature request</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FeatureRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeatureRequestDto>> SubmitFeatureRequest(
        [FromBody] SubmitFeatureRequestCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature request submitted: {RequestId}", result.Id);
            return CreatedAtAction(nameof(SubmitFeatureRequest), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit feature request");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all pending feature requests.
    /// </summary>
    /// <param name="limit">Maximum number of requests to return</param>
    /// <param name="offset">Number of requests to skip</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending feature requests</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<FeatureRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureRequestDto>>> GetPendingRequests(
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingRequestsQuery { Limit = limit, Offset = offset };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all feature requests with a specific status.
    /// </summary>
    /// <param name="status">Request status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of feature requests with the specified status</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<FeatureRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<FeatureRequestDto>>> GetRequestsByStatus(
        RequestStatus status,
        CancellationToken cancellationToken)
    {
        var query = new GetRequestsByStatusQuery(status);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all feature requests linked to a specific feature.
    /// </summary>
    /// <param name="featureId">Feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of feature requests linked to the feature</returns>
    [HttpGet("feature/{featureId}")]
    [ProducesResponseType(typeof(IEnumerable<FeatureRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureRequestDto>>> GetRequestsByFeature(
        Guid featureId,
        CancellationToken cancellationToken)
    {
        var query = new GetRequestsByFeatureQuery(featureId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all feature requests marked as duplicates of a specific request.
    /// </summary>
    /// <param name="requestId">Original request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of duplicate requests</returns>
    [HttpGet("{requestId}/duplicates")]
    [ProducesResponseType(typeof(IEnumerable<FeatureRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureRequestDto>>> GetDuplicateRequests(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var query = new GetDuplicateRequestsQuery(requestId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Link a feature request to an existing feature.
    /// </summary>
    /// <param name="requestId">Feature request ID</param>
    /// <param name="command">Link details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpPost("{requestId}/link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LinkRequestToFeature(
        Guid requestId,
        [FromBody] LinkRequestToFeatureCommand command,
        CancellationToken cancellationToken)
    {
        if (requestId != command.RequestId)
        {
            return BadRequest("Request ID in URL does not match request ID in body");
        }

        try
        {
            await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature request {RequestId} linked to feature {FeatureId}", 
                requestId, command.FeatureId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mark a feature request as a duplicate of another request.
    /// </summary>
    /// <param name="requestId">Feature request ID to mark as duplicate</param>
    /// <param name="command">Duplicate details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpPost("{requestId}/mark-duplicate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkRequestAsDuplicate(
        Guid requestId,
        [FromBody] MarkRequestAsDuplicateCommand command,
        CancellationToken cancellationToken)
    {
        if (requestId != command.RequestId)
        {
            return BadRequest("Request ID in URL does not match request ID in body");
        }

        try
        {
            await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature request {RequestId} marked as duplicate of {DuplicateId}", 
                requestId, command.DuplicateOfRequestId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update the status of a feature request.
    /// </summary>
    /// <param name="requestId">Feature request ID</param>
    /// <param name="command">Status update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpPatch("{requestId}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRequestStatus(
        Guid requestId,
        [FromBody] UpdateRequestStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (requestId != command.RequestId)
        {
            return BadRequest("Request ID in URL does not match request ID in body");
        }

        try
        {
            await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Feature request {RequestId} status updated to {Status}", 
                requestId, command.Status);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
}
