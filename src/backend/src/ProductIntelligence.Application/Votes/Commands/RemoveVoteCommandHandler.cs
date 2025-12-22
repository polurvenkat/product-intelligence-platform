using MediatR;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Votes.Commands;

public class RemoveVoteCommandHandler : IRequestHandler<RemoveVoteCommand, bool>
{
    private readonly IFeatureVoteRepository _voteRepository;

    public RemoveVoteCommandHandler(IFeatureVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<bool> Handle(RemoveVoteCommand request, CancellationToken cancellationToken)
    {
        await _voteRepository.DeleteAsync(request.FeatureId, request.VoterEmail, cancellationToken);
        return true;
    }
}
