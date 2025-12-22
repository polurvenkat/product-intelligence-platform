using MediatR;

namespace ProductIntelligence.Application.Commands.FeatureRequests;

/// <summary>
/// Command to mark a feature request as a duplicate of another request.
/// </summary>
public record MarkRequestAsDuplicateCommand : IRequest<bool>
{
    public Guid RequestId { get; init; }
    public Guid DuplicateOfRequestId { get; init; }
    public decimal SimilarityScore { get; init; }
}
