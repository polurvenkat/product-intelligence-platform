using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Votes.Queries;

public class GetVotesByFeatureQueryHandler : IRequestHandler<GetVotesByFeatureQuery, IEnumerable<FeatureVoteDto>>
{
    private readonly IFeatureVoteRepository _voteRepository;

    public GetVotesByFeatureQueryHandler(IFeatureVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<IEnumerable<FeatureVoteDto>> Handle(GetVotesByFeatureQuery request, CancellationToken cancellationToken)
    {
        var votes = await _voteRepository.GetByFeatureIdAsync(request.FeatureId, cancellationToken);

        return votes.Select(v => new FeatureVoteDto
        {
            Id = v.Id,
            FeatureId = v.FeatureId,
            FeatureRequestId = v.FeatureRequestId,
            VoterEmail = v.VoterEmail,
            VoterCompany = v.VoterCompany,
            VoterTier = v.VoterTier,
            VoteWeight = v.VoteWeight,
            VotedAt = v.VotedAt
        });
    }
}
