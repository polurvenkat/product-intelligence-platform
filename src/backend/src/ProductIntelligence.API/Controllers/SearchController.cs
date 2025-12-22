using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Queries.Search;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.API.Controllers;

/// <summary>
/// Provides semantic search and filtering capabilities for features and requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SearchController> _logger;

    public SearchController(IMediator mediator, ILogger<SearchController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Perform semantic search on features using natural language.
    /// </summary>
    /// <param name="q">Search query text</param>
    /// <param name="threshold">Similarity threshold (0-1), default 0.7</param>
    /// <param name="limit">Maximum number of results, default 20</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of similar features with relevance scores</returns>
    [HttpGet("features")]
    [ProducesResponseType(typeof(IEnumerable<FeatureSearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<FeatureSearchResult>>> SearchFeatures(
        [FromQuery] string q,
        [FromQuery] double threshold = 0.7,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Search query 'q' is required" });
        }

        if (threshold < 0 || threshold > 1)
        {
            return BadRequest(new { error = "Threshold must be between 0 and 1" });
        }

        if (limit < 1 || limit > 100)
        {
            return BadRequest(new { error = "Limit must be between 1 and 100" });
        }

        _logger.LogInformation("Searching features with query: {Query}", q);

        var query = new SearchFeaturesQuery
        {
            SearchText = q,
            Threshold = threshold,
            Limit = limit
        };

        var results = await _mediator.Send(query, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Perform semantic search on feature requests using natural language.
    /// </summary>
    /// <param name="q">Search query text</param>
    /// <param name="threshold">Similarity threshold (0-1), default 0.7</param>
    /// <param name="limit">Maximum number of results, default 20</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of similar feature requests with relevance scores</returns>
    [HttpGet("feature-requests")]
    [ProducesResponseType(typeof(IEnumerable<FeatureRequestSearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<FeatureRequestSearchResult>>> SearchFeatureRequests(
        [FromQuery] string q,
        [FromQuery] double threshold = 0.7,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Search query 'q' is required" });
        }

        if (threshold < 0 || threshold > 1)
        {
            return BadRequest(new { error = "Threshold must be between 0 and 1" });
        }

        if (limit < 1 || limit > 100)
        {
            return BadRequest(new { error = "Limit must be between 1 and 100" });
        }

        _logger.LogInformation("Searching feature requests with query: {Query}", q);

        var query = new SearchFeatureRequestsQuery
        {
            SearchText = q,
            Threshold = threshold,
            Limit = limit
        };

        var results = await _mediator.Send(query, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Filter features with complex criteria.
    /// </summary>
    /// <param name="domainId">Filter by domain ID</param>
    /// <param name="status">Filter by status</param>
    /// <param name="priority">Filter by priority</param>
    /// <param name="createdAfter">Filter by creation date (after)</param>
    /// <param name="createdBefore">Filter by creation date (before)</param>
    /// <param name="minPriorityScore">Minimum AI priority score</param>
    /// <param name="maxPriorityScore">Maximum AI priority score</param>
    /// <param name="targetRelease">Filter by target release</param>
    /// <param name="limit">Maximum number of results, default 100</param>
    /// <param name="offset">Number of results to skip, default 0</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of features matching the criteria</returns>
    [HttpGet("features/filter")]
    [ProducesResponseType(typeof(IEnumerable<FeatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<FeatureDto>>> FilterFeatures(
        [FromQuery] Guid? domainId = null,
        [FromQuery] FeatureStatus? status = null,
        [FromQuery] Priority? priority = null,
        [FromQuery] DateTime? createdAfter = null,
        [FromQuery] DateTime? createdBefore = null,
        [FromQuery] decimal? minPriorityScore = null,
        [FromQuery] decimal? maxPriorityScore = null,
        [FromQuery] string? targetRelease = null,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 500)
        {
            return BadRequest(new { error = "Limit must be between 1 and 500" });
        }

        if (offset < 0)
        {
            return BadRequest(new { error = "Offset must be non-negative" });
        }

        _logger.LogInformation("Filtering features with criteria");

        var query = new FilterFeaturesQuery
        {
            DomainId = domainId,
            Status = status,
            Priority = priority,
            CreatedAfter = createdAfter,
            CreatedBefore = createdBefore,
            MinPriorityScore = minPriorityScore,
            MaxPriorityScore = maxPriorityScore,
            TargetRelease = targetRelease,
            Limit = limit,
            Offset = offset
        };

        var results = await _mediator.Send(query, cancellationToken);
        return Ok(results);
    }
}
