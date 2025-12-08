using MediatR;
using ProductIntelligence.Application.Commands.Domains;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Interfaces.Repositories;

namespace ProductIntelligence.Application.Commands.Domains;

public class CreateDomainCommandHandler : IRequestHandler<CreateDomainCommand, DomainDto>
{
    private readonly IDomainRepository _domainRepository;

    public CreateDomainCommandHandler(IDomainRepository domainRepository)
    {
        _domainRepository = domainRepository;
    }

    public async Task<DomainDto> Handle(CreateDomainCommand request, CancellationToken cancellationToken)
    {
        string? parentPath = null;
        
        if (request.ParentDomainId.HasValue)
        {
            var parentDomain = await _domainRepository.GetByIdAsync(request.ParentDomainId.Value, cancellationToken);
            if (parentDomain == null)
            {
                throw new ArgumentException($"Parent domain {request.ParentDomainId} not found");
            }
            parentPath = parentDomain.Path;
        }

        var domain = new Domain(
            request.OrganizationId,
            request.Name,
            request.Description,
            request.ParentDomainId,
            parentPath,
            request.OwnerUserId);

        await _domainRepository.AddAsync(domain, cancellationToken);

        return new DomainDto
        {
            Id = domain.Id,
            OrganizationId = domain.OrganizationId,
            ParentDomainId = domain.ParentDomainId,
            Name = domain.Name,
            Description = domain.Description,
            Path = domain.Path,
            Level = domain.GetLevel(),
            FeatureCount = 0,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }
}
