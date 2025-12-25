using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Votes.Commands;
using ProductIntelligence.Application.Votes.Queries;

namespace ProductIntelligence.API.Controllers;

/// <summary>
/// Manages voting for features and feature requests.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VotesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VotesController> _logger;

    public VotesController(IMediator mediator, ILogger<VotesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Vote for a feature or feature request.
    /// </summary>
    /// <param name="command">Vote details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of the created vote</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> VoteForFeature(
        [FromBody] VoteForFeatureCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recording vote for {Type} {Id} from {Email}", 
            command.FeatureId.HasValue ? "Feature" : "FeatureRequest",
            command.FeatureId ?? command.FeatureRequestId,
            command.VoterEmail);

        var voteId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetVotesByFeature),
            new { featureId = command.FeatureId ?? command.FeatureRequestId },
            voteId);
    }

    /// <summary>
    /// Remove a vote for a feature.
    /// </summary>
    /// <param name="featureId">Feature ID</param>
    /// <param name="voterEmail">Voter email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{featureId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveVote(
        Guid featureId,
        [FromQuery] string voterEmail,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(voterEmail))
        {
            return BadRequest("Voter email is required");
        }

        _logger.LogInformation("Removing vote for Feature {FeatureId} from {Email}", featureId, voterEmail);

        await _mediator.Send(new RemoveVoteCommand(featureId, voterEmail), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get all votes for a specific feature.
    /// </summary>
    /// <param name="featureId">Feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of votes</returns>
    [HttpGet("feature/{featureId}")]
    [ProducesResponseType(typeof(IEnumerable<FeatureVoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureVoteDto>>> GetVotesByFeature(
        Guid featureId,
        CancellationToken cancellationToken)
    {
        var votes = await _mediator.Send(new GetVotesByFeatureQuery(featureId), cancellationToken);
        return Ok(votes);
    }

    /// <summary>
    /// Get vote count for a specific feature.
    /// </summary>
    /// <param name="featureId">Feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vote count details</returns>
    [HttpGet("feature/{featureId}/count")]
    [ProducesResponseType(typeof(VoteCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VoteCountDto>> GetVoteCount(
        Guid featureId,
        CancellationToken cancellationToken)
    {
        var voteCount = await _mediator.Send(new GetVoteCountQuery(featureId), cancellationToken);
        return Ok(voteCount);
    }
}
