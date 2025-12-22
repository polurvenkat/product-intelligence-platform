using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Queries.Domains;

/// <summary>
/// Query to retrieve a single domain by ID
/// </summary>
public record GetDomainQuery : IRequest<DomainDto?>
{
    public Guid Id { get; init; }
}
