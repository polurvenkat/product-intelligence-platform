using MediatR;
using ProductIntelligence.Application.Commands.FeatureRequests;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.FeatureRequests;

public class GetRequestsByStatusQueryHandler : IRequestHandler<GetRequestsByStatusQuery, IEnumerable<FeatureRequestDto>>
{
    private readonly IFeatureRequestRepository _requestRepository;

    public GetRequestsByStatusQueryHandler(IFeatureRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<IEnumerable<FeatureRequestDto>> Handle(GetRequestsByStatusQuery request, CancellationToken cancellationToken)
    {
        var requests = await _requestRepository.GetByStatusAsync(request.Status, cancellationToken);

        return requests.Select(r => new FeatureRequestDto
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
