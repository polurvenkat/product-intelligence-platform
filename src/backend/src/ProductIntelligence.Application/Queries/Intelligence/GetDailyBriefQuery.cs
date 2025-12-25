using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Queries.Intelligence;

public record GetDailyBriefQuery : IRequest<IEnumerable<FeatureSearchResult>>
{
    public int Limit { get; init; } = 5;
}

public class GetDailyBriefHandler : IRequestHandler<GetDailyBriefQuery, IEnumerable<FeatureSearchResult>>
{
    private readonly IFeatureRepository _featureRepository;

    public GetDailyBriefHandler(IFeatureRepository featureRepository)
    {
        _featureRepository = featureRepository;
    }

    public async Task<IEnumerable<FeatureSearchResult>> Handle(GetDailyBriefQuery request, CancellationToken cancellationToken)
    {
        var topFeatures = await _featureRepository.GetTopFeaturesAsync(request.Limit, cancellationToken);
        
        return topFeatures.Select(f => new FeatureSearchResult
        {
            Id = f.Id,
            DomainId = f.DomainId,
            DomainName = "Unknown", // We could join to get this if needed
            Title = f.Title,
            Description = f.Description,
            Status = f.Status,
            Priority = f.Priority,
            AiPriorityScore = f.AiPriorityScore,
            VoteCount = f.VoteCount,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt
        });
    }
}
