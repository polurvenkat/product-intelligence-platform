using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductIntelligence.Application.Commands.Domains;
using ProductIntelligence.Application.Queries.Domains;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DomainsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainsController> _logger;

    public DomainsController(IMediator mediator, ILogger<DomainsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DomainDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DomainDto>> CreateDomain(
        [FromBody] CreateDomainCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetDomain), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DomainDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DomainDto>> UpdateDomain(
        [FromRoute] Guid id,
        [FromBody] UpdateDomainCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "ID mismatch between route and body" });
        }

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Domain updated: {DomainId}", result.Id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update domain");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteDomain(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteDomainCommand { Id = id };
            await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Domain deleted: {DomainId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DomainDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DomainDto>> GetDomain(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetDomainQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result == null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }

    [HttpGet("organization/{organizationId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DomainDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DomainDto>>> GetDomainHierarchy(
        [FromRoute] Guid organizationId,
        CancellationToken cancellationToken)
    {
        var query = new GetDomainHierarchyQuery { OrganizationId = organizationId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
