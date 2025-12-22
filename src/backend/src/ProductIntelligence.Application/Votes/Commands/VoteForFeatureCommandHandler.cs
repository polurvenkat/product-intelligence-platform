using MediatR;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Votes.Commands;

public class VoteForFeatureCommandHandler : IRequestHandler<VoteForFeatureCommand, Guid>
{
    private readonly IFeatureVoteRepository _voteRepository;

    public VoteForFeatureCommandHandler(IFeatureVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<Guid> Handle(VoteForFeatureCommand request, CancellationToken cancellationToken)
    {
        var vote = new FeatureVote(
            request.VoterEmail,
            request.VoterTier,
            request.FeatureId,
            request.FeatureRequestId,
            request.VoterCompany);

        return await _voteRepository.AddAsync(vote, cancellationToken);
    }
}
