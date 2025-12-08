using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.Commands.FeatureRequests;

namespace ProductIntelligence.API.Controllers;

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

    [HttpPost]
    [ProducesResponseType(typeof(FeatureRequestDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeatureRequestDto>> SubmitFeatureRequest(
        [FromBody] SubmitFeatureRequestCommand command,
        CancellationToken cancellationToken)
    {
        // Placeholder - implement handler
        return Accepted();
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<FeatureRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeatureRequestDto>>> GetPendingRequests(
        CancellationToken cancellationToken)
    {
        // Placeholder
        return Ok(Array.Empty<FeatureRequestDto>());
    }
}
