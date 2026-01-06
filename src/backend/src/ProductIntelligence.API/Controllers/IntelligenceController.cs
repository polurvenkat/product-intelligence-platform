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
[AllowAnonymous]
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
            // Extract userid and organizationId from query if provided for now
            if (Guid.TryParse(Request.Query["userId"], out var userId))
            {
                command.UserId = userId;
            }
            if (Guid.TryParse(Request.Query["organizationId"], out var orgId))
            {
                command.OrganizationId = orgId;
            }
            
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
    /// Analyzes a GitHub repository to suggest a domain structure using AI
    /// </summary>
    /// <param name="command">GitHub repository analysis parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Suggested domain structure and summary</returns>
    [HttpPost("analyze-github")]
    [ProducesResponseType(typeof(DocumentAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentAnalysisResultDto>> AnalyzeGitHub(
        [FromBody] AnalyzeGitHubRepoCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RepoUrl))
        {
            return BadRequest(new { error = "Repository URL is required" });
        }

        try
        {
            // Extract userid and organizationId from query if provided for now
            if (!command.UserId.HasValue && Guid.TryParse(Request.Query["userId"], out var userId))
            {
                command.UserId = userId;
            }
            if (!command.OrganizationId.HasValue && Guid.TryParse(Request.Query["organizationId"], out var orgId))
            {
                command.OrganizationId = orgId;
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing GitHub repository: {RepoUrl}", command.RepoUrl);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Analyzes a video/meeting recording to suggest a domain structure using AI
    /// </summary>
    /// <param name="file">The video or audio file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Suggested domain structure and summary</returns>
    [HttpPost("analyze-video")]
    [ProducesResponseType(typeof(DocumentAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentAnalysisResultDto>> AnalyzeVideo(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        try
        {
            var command = new AnalyzeVideoCommand { File = file };
            
            if (Guid.TryParse(Request.Query["userId"], out var userId))
            {
                command.UserId = userId;
            }
            if (Guid.TryParse(Request.Query["organizationId"], out var orgId))
            {
                command.OrganizationId = orgId;
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing video");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Interactive chat for product discovery and strategy
    /// </summary>
    /// <param name="command">The chat message and history</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response and suggested actions</returns>
    [HttpPost("discovery-chat")]
    [ProducesResponseType(typeof(DiscoveryChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DiscoveryChatResponseDto>> DiscoveryChat(
        [FromBody] DiscoveryChatCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Message))
        {
            return BadRequest(new { error = "Message cannot be empty" });
        }

        try
        {
            // If UserId/OrgId isn't in body but is in query, populate it
            if (!command.UserId.HasValue && Guid.TryParse(Request.Query["userId"], out var userId))
            {
                command.UserId = userId;
            }
            if (!command.OrganizationId.HasValue && Guid.TryParse(Request.Query["organizationId"], out var orgId))
            {
                command.OrganizationId = orgId;
            }
            
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in discovery chat");
            return StatusCode(500, new { error = "An error occurred during the chat. Please try again." });
        }
    }

    /// <summary>
    /// Gets the chat history for a discovery session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of chat messages</returns>
    [HttpGet("chat-history/{sessionId}")]
    [ProducesResponseType(typeof(IEnumerable<ChatTurnDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatTurnDto>>> GetChatHistory(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var query = new GetChatHistoryQuery { SessionId = sessionId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets discovery analytics including bucket health and competitive trends.
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discovery analytics results</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(DiscoveryAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscoveryAnalyticsDto>> GetAnalytics(
        [FromQuery] string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDiscoveryAnalyticsQuery { OrganizationId = organizationId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
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
