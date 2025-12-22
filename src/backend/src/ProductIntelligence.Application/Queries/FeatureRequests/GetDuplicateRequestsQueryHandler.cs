using MediatR;
using ProductIntelligence.Application.Commands.FeatureRequests;
using ProductIntelligence.Core.Enums;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.FeatureRequests;

public class GetDuplicateRequestsQueryHandler : IRequestHandler<GetDuplicateRequestsQuery, IEnumerable<FeatureRequestDto>>
{
    private readonly IFeatureRequestRepository _requestRepository;

    public GetDuplicateRequestsQueryHandler(IFeatureRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<IEnumerable<FeatureRequestDto>> Handle(GetDuplicateRequestsQuery request, CancellationToken cancellationToken)
    {
        // Get all duplicate requests
        var allRequests = await _requestRepository.GetByStatusAsync(RequestStatus.Duplicate, cancellationToken);

        // Filter to only those that are duplicates of the specified request
        var duplicates = allRequests.Where(r => r.DuplicateOfRequestId == request.OriginalRequestId);

        return duplicates.Select(r => new FeatureRequestDto
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Source = r.Source,
            SourceId = r.SourceId,
            RequesterName = r.RequesterName,
            RequesterEmail = r.RequesterEmail,
            RequesterCompany = r.RequesterCompany,
            RequesterTier = r.RequesterTier,
            SubmittedAt = r.SubmittedAt,
            Status = r.Status,
            ProcessedAt = r.ProcessedAt,
            LinkedFeatureId = r.LinkedFeatureId,
            DuplicateOfRequestId = r.DuplicateOfRequestId,
            SimilarityScore = r.SimilarityScore
        });
    }
}
