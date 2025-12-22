using MediatR;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.Domains;

public class UpdateDomainCommandHandler : IRequestHandler<UpdateDomainCommand, DomainDto>
{
    private readonly IDomainRepository _domainRepository;

    public UpdateDomainCommandHandler(IDomainRepository domainRepository)
    {
        _domainRepository = domainRepository ?? throw new ArgumentNullException(nameof(domainRepository));
    }

    public async Task<DomainDto> Handle(UpdateDomainCommand request, CancellationToken cancellationToken)
    {
        var domain = await _domainRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (domain == null)
        {
            throw new KeyNotFoundException($"Domain with ID {request.Id} not found");
        }

        domain.Update(request.Name, request.Description, request.OwnerUserId);
        await _domainRepository.UpdateAsync(domain, cancellationToken);

        var featureCount = await _domainRepository.GetFeatureCountAsync(domain.Id, cancellationToken);
        var hasChildren = await _domainRepository.HasChildrenAsync(domain.Id, cancellationToken);

        return new DomainDto
        {
            Id = domain.Id,
            OrganizationId = domain.OrganizationId,
            Name = domain.Name,
            Description = domain.Description ?? string.Empty,
            Path = domain.Path,
            ParentId = domain.ParentDomainId,
            Level = domain.GetLevel(),
            FeatureCount = featureCount,
            HasChildren = hasChildren,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }
}
