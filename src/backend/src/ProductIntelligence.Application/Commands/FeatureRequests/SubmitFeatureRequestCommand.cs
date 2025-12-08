using MediatR;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

public record SubmitFeatureRequestCommand : IRequest<FeatureRequestDto>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public string? RequesterEmail { get; init; }
    public string? RequesterCompany { get; init; }
    public CustomerTier RequesterTier { get; init; } = CustomerTier.Starter;
    public RequestSource Source { get; init; } = RequestSource.Manual;
    public string? SourceId { get; init; }
}

public record FeatureRequestDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public string? RequesterCompany { get; init; }
    public CustomerTier RequesterTier { get; init; }
    public RequestStatus Status { get; init; }
    public DateTime SubmittedAt { get; init; }
}
