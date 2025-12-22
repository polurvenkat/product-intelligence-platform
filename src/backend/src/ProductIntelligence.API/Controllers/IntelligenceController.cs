using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.Commands.Intelligence;

namespace ProductIntelligence.API.Controllers;

/// <summary>
/// AI-powered intelligence endpoints for feature analysis and recommendations
/// </summary>
[ApiController]
[Route("api/intelligence")]
[Produces("application/json")]
public class IntelligenceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IntelligenceController> _logger;

    public IntelligenceController(
        IMediator mediator,
        ILogger<IntelligenceController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes a feature request to find potential duplicates or similar requests using AI
    /// </summary>
    /// <param name="command">Feature request analysis parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deduplication analysis results with confidence scores</returns>
    /// <response code="200">Analysis completed successfully</response>
    /// <response code="400">Invalid input parameters</response>
    /// <response code="500">Internal server error during analysis</response>
    [HttpPost("analyze-request")]
    [ProducesResponseType(typeof(DeduplicationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeduplicationResultDto>> AnalyzeFeatureRequest(
        [FromBody] AnalyzeFeatureRequestCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Received deduplication analysis request for: {Title}",
                command.Title);

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Deduplication analysis completed. Duplicates: {HasDuplicates}, Similar: {HasSimilar}",
                result.HasDuplicates,
                result.HasSimilar);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for deduplication analysis");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deduplication analysis");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Analysis Error",
                Detail = "An error occurred while analyzing the feature request. Please try again.",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Health check endpoint for intelligence services
    /// </summary>
    /// <returns>Service status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "Intelligence",
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
