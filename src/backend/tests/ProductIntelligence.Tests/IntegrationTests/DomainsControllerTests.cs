using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductIntelligence.Application.Commands.Domains;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Tests.IntegrationTests;

public class DomainsControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateDomain_WithValidData_ReturnsCreatedDomain()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var command = new CreateDomainCommand
        {
            OrganizationId = organizationId,
            Name = "Customer Management",
            Description = "Handles all customer-related functionality"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/domains", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var domain = await response.Content.ReadFromJsonAsync<DomainDto>();
        domain.Should().NotBeNull();
        domain!.Id.Should().NotBeEmpty();
        domain.Name.Should().Be("Customer Management");
        domain.Description.Should().Be("Handles all customer-related functionality");
        domain.OrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public async Task CreateDomain_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateDomainCommand
        {
            OrganizationId = Guid.NewGuid(),
            Name = "",
            Description = "Test description"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/domains", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDomain_WithParentDomain_CreatesHierarchy()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        
        // Create parent domain
        var parentCommand = new CreateDomainCommand
        {
            OrganizationId = organizationId,
            Name = "Product Management",
            Description = "Product domain"
        };
        var parentResponse = await Client.PostAsJsonAsync("/api/domains", parentCommand);
        var parentDomain = await parentResponse.Content.ReadFromJsonAsync<DomainDto>();

        // Create child domain
        var childCommand = new CreateDomainCommand
        {
            OrganizationId = organizationId,
            Name = "Feature Planning",
            Description = "Feature planning sub-domain",
            ParentDomainId = parentDomain!.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/domains", childCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var childDomain = await response.Content.ReadFromJsonAsync<DomainDto>();
        childDomain.Should().NotBeNull();
        childDomain!.ParentId.Should().Be(parentDomain.Id);
        childDomain.Name.Should().Be("Feature Planning");
    }

    [Fact]
    public async Task GetDomain_WithExistingId_ReturnsDomain()
    {
        // Arrange
        var command = new CreateDomainCommand
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test Domain",
            Description = "Test"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/domains", command);
        var createdDomain = await createResponse.Content.ReadFromJsonAsync<DomainDto>();

        // Act
        var response = await Client.GetAsync($"/api/domains/{createdDomain!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var domain = await response.Content.ReadFromJsonAsync<DomainDto>();
        domain.Should().NotBeNull();
        domain!.Id.Should().Be(createdDomain.Id);
        domain.Name.Should().Be("Test Domain");
    }

    [Fact]
    public async Task GetDomain_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/domains/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateDomain_WithValidData_UpdatesDomain()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var createCommand = new CreateDomainCommand
        {
            OrganizationId = organizationId,
            Name = "Original Name",
            Description = "Original Description"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/domains", createCommand);
        var domain = await createResponse.Content.ReadFromJsonAsync<DomainDto>();

        var updateCommand = new UpdateDomainCommand
        {
            Id = domain!.Id,
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/domains/{domain.Id}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedDomain = await response.Content.ReadFromJsonAsync<DomainDto>();
        updatedDomain.Should().NotBeNull();
        updatedDomain!.Name.Should().Be("Updated Name");
        updatedDomain.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateDomain_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var command = new UpdateDomainCommand
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Description = "Test"
        };
        var differentId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/domains/{differentId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteDomain_WithExistingId_DeletesDomain()
    {
        // Arrange
        var command = new CreateDomainCommand
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Domain to Delete",
            Description = "Will be deleted"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/domains", command);
        var domain = await createResponse.Content.ReadFromJsonAsync<DomainDto>();

        // Act
        var response = await Client.DeleteAsync($"/api/domains/{domain!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"/api/domains/{domain.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDomain_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/domains/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDomainHierarchy_ReturnsAllDomains()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        
        await Client.PostAsJsonAsync("/api/domains", new CreateDomainCommand
        {
            OrganizationId = organizationId,
            Name = "Domain 1",
            Description = "First domain"
        });

        await Client.PostAsJsonAsync("/api/domains", new CreateDomainCommand
        {
            OrganizationId = organizationId,
            Name = "Domain 2",
            Description = "Second domain"
        });

        // Act
        var response = await Client.GetAsync($"/api/domains/organization/{organizationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var domains = await response.Content.ReadFromJsonAsync<List<DomainDto>>();
        domains.Should().NotBeNull();
        domains!.Should().HaveCount(2);
        domains.Should().Contain(d => d.Name == "Domain 1");
        domains.Should().Contain(d => d.Name == "Domain 2");
    }
}
