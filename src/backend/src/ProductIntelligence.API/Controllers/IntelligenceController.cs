using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.Commands.Intelligence;
using ProductIntelligence.Application.Queries.Intelligence;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.API.Controllers;

/// <summary>
/// AI-powered intelligence endpoints for feature analysis and recommendations
/// </summary>
[Authorize]
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
    /// Gets a daily brief of top prioritized features and insights.
    /// </summary>
    /// <param name="limit">Number of features to include</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Daily brief results</returns>
    [HttpGet("daily-brief")]
    [ProducesResponseType(typeof(IEnumerable<FeatureSearchResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureSearchResult>>> GetDailyBrief(
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDailyBriefQuery { Limit = limit };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
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
    /// Analyzes a document to suggest a domain structure using AI
    /// </summary>
    /// <param name="file">The document file to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Suggested domain structure and summary</returns>
    [HttpPost("analyze-document")]
    [ProducesResponseType(typeof(DocumentAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentAnalysisResultDto>> AnalyzeDocument(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        try
        {
            var command = new AnalyzeDocumentCommand { File = file };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document");
            return BadRequest(new { error = ex.Message });
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
