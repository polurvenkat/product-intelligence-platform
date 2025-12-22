using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Votes.Queries;

public class GetVoteCountQueryHandler : IRequestHandler<GetVoteCountQuery, VoteCountDto>
{
    private readonly IFeatureVoteRepository _voteRepository;

    public GetVoteCountQueryHandler(IFeatureVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<VoteCountDto> Handle(GetVoteCountQuery request, CancellationToken cancellationToken)
    {
        var voteCount = await _voteRepository.GetVoteCountAsync(request.FeatureId, cancellationToken);

        return new VoteCountDto
        {
            Count = voteCount.Count,
            WeightedCount = voteCount.WeightedCount
        };
    }
}
