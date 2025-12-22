using MediatR;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.Features;

public class DeleteFeatureCommandHandler : IRequestHandler<DeleteFeatureCommand, bool>
{
    private readonly IFeatureRepository _featureRepository;

    public DeleteFeatureCommandHandler(IFeatureRepository featureRepository)
    {
        _featureRepository = featureRepository ?? throw new ArgumentNullException(nameof(featureRepository));
    }

    public async Task<bool> Handle(DeleteFeatureCommand request, CancellationToken cancellationToken)
    {
        var feature = await _featureRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (feature == null)
        {
            throw new KeyNotFoundException($"Feature with ID {request.Id} not found");
        }

        await _featureRepository.DeleteAsync(request.Id, cancellationToken);
        return true;
    }
}
