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

        // Cascading delete is handled in the repository
        await _domainRepository.DeleteAsync(request.Id, cancellationToken);
        return true;
    }
}
