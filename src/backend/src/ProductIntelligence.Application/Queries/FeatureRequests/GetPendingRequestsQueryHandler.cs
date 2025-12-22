using MediatR;
using ProductIntelligence.Application.Commands.FeatureRequests;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.FeatureRequests;

/// <summary>
/// Handler for retrieving pending feature requests
/// </summary>
public class GetPendingRequestsQueryHandler : IRequestHandler<GetPendingRequestsQuery, IEnumerable<FeatureRequestDto>>
{
    private readonly IFeatureRequestRepository _requestRepository;

    public GetPendingRequestsQueryHandler(IFeatureRequestRepository requestRepository)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
    }

    public async Task<IEnumerable<FeatureRequestDto>> Handle(GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await _requestRepository.GetPendingAsync(cancellationToken);
        
        var result = requests
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(r => new FeatureRequestDto
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
                Status = r.Status,
                SubmittedAt = r.SubmittedAt,
                ProcessedAt = r.ProcessedAt,
                LinkedFeatureId = r.LinkedFeatureId,
                DuplicateOfRequestId = r.DuplicateOfRequestId,
                SimilarityScore = r.SimilarityScore
            })
            .ToList();

        return result;
    }
}
