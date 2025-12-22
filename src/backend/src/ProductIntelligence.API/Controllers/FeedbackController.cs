using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Feedback.Commands;
using ProductIntelligence.Application.Feedback.Queries;

namespace ProductIntelligence.API.Controllers;

/// <summary>
/// Manages customer feedback on features and feature requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(IMediator mediator, ILogger<FeedbackController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Submit new feedback for a feature or feature request.
    /// </summary>
    /// <param name="command">Feedback submission details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of the created feedback</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> SubmitFeedback(
        [FromBody] SubmitFeedbackCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting feedback for {Type} {Id}", 
            command.FeatureId.HasValue ? "Feature" : "FeatureRequest",
            command.FeatureId ?? command.FeatureRequestId);

        var feedbackId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetFeedback),
            new { id = feedbackId },
            feedbackId);
    }

    /// <summary>
    /// Get feedback by ID.
    /// </summary>
    /// <param name="id">Feedback ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Feedback details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackDto>> GetFeedback(
        Guid id,
        CancellationToken cancellationToken)
    {
        var feedback = await _mediator.Send(new GetFeedbackQuery(id), cancellationToken);

        if (feedback == null)
        {
            _logger.LogWarning("Feedback not found: {FeedbackId}", id);
            return NotFound();
        }

        return Ok(feedback);
    }

    /// <summary>
    /// Get all feedback for a specific feature.
    /// </summary>
    /// <param name="featureId">Feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of feedback</returns>
    [HttpGet("feature/{featureId}")]
    [ProducesResponseType(typeof(IEnumerable<FeedbackDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeedbackDto>>> GetFeedbackByFeature(
        Guid featureId,
        CancellationToken cancellationToken)
    {
        var feedback = await _mediator.Send(new GetFeedbackByFeatureQuery(featureId), cancellationToken);
        return Ok(feedback);
    }

    /// <summary>
    /// Get all feedback for a specific feature request.
    /// </summary>
    /// <param name="requestId">Feature request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of feedback</returns>
    [HttpGet("request/{requestId}")]
    [ProducesResponseType(typeof(IEnumerable<FeedbackDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeedbackDto>>> GetFeedbackByRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var feedback = await _mediator.Send(new GetFeedbackByRequestQuery(requestId), cancellationToken);
        return Ok(feedback);
    }

    /// <summary>
    /// Delete feedback.
    /// </summary>
    /// <param name="id">Feedback ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeedback(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteFeedbackCommand(id), cancellationToken);
            _logger.LogInformation("Feedback deleted: {FeedbackId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Feedback not found for deletion: {FeedbackId}", id);
            return NotFound();
        }
    }
}
