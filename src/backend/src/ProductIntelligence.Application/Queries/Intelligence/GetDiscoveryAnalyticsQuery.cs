using MediatR;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Application.Queries.Intelligence;

public class GetDiscoveryAnalyticsQuery : IRequest<DiscoveryAnalyticsDto>
{
    public string? OrganizationId { get; set; }
}
