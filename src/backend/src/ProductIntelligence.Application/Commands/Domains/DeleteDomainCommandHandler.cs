using MediatR;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.Domains;

public class DeleteDomainCommandHandler : IRequestHandler<DeleteDomainCommand, bool>
{
    private readonly IDomainRepository _domainRepository;

    public DeleteDomainCommandHandler(IDomainRepository domainRepository)
    {
        _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
    }

    public async Task<bool> Handle(DeleteDomainCommand request, CancellationToken cancellationToken)
    {
        var domain = await _domainRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (domain == null)
        {
            throw new KeyNotFoundException($"Domain with ID {request.Id} not found");
        }

        // Check if domain has children
        var hasChildren = await _domainRepository.HasChildrenAsync(request.Id, cancellationToken);
        if (hasChildren)
        {
            throw new InvalidOperationException("Cannot delete domain with child domains. Delete children first.");
        }

        // Check if domain has features
        var featureCount = await _domainRepository.GetFeatureCountAsync(request.Id, cancellationToken);
        if (featureCount > 0)
        {
            throw new InvalidOperationException($"Cannot delete domain with {featureCount} features. Remove or reassign features first.");
        }

        await _domainRepository.DeleteAsync(request.Id, cancellationToken);
        return true;
    }
}
