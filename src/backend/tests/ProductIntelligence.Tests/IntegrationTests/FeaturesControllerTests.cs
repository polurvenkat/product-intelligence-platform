using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductIntelligence.Application.Commands.Domains;
using ProductIntelligence.Application.Commands.Features;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Tests.IntegrationTests;

public class FeaturesControllerTests : IntegrationTestBase
{
    private async Task<Guid> CreateTestDomainAsync()
    {
        var command = new CreateDomainCommand
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test Domain",
            Description = "Test domain for features"
        };
        var response = await Client.PostAsJsonAsync("/api/domains", command);
        var domain = await response.Content.ReadFromJsonAsync<DomainDto>();
        return domain!.Id;
    }

    [Fact]
    public async Task CreateFeature_WithValidData_ReturnsCreatedFeature()
    {
        // Arrange
        var domainId = await CreateTestDomainAsync();
        var command = new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Dark Mode Support",
            Description = "Add dark mode theme to the application",
            Priority = Priority.P2,
            CreatedBy = Guid.NewGuid()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/features", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var feature = await response.Content.ReadFromJsonAsync<FeatureDto>();
        feature.Should().NotBeNull();
        feature!.Id.Should().NotBeEmpty();
        feature.Title.Should().Be("Dark Mode Support");
        feature.Description.Should().Be("Add dark mode theme to the application");
        feature.Priority.Should().Be(Priority.P2);
        feature.Status.Should().Be(FeatureStatus.Proposed);
    }

    [Fact]
    public async Task CreateFeature_WithInvalidDomain_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateFeatureCommand
        {
            DomainId = Guid.NewGuid(), // Non-existent domain
            Title = "Test Feature",
            Description = "Test",
            CreatedBy = Guid.NewGuid()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/features", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeature_WithExistingId_ReturnsFeature()
    {
        // Arrange
        var domainId = await CreateTestDomainAsync();
        var createCommand = new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Export to CSV",
            Description = "Allow users to export data to CSV format",
            CreatedBy = Guid.NewGuid()
        };
        var createResponse = await Client.PostAsJsonAsync("/api/features", createCommand);
        var createdFeature = await createResponse.Content.ReadFromJsonAsync<FeatureDto>();

        // Act
        var response = await Client.GetAsync($"/api/features/{createdFeature!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var feature = await response.Content.ReadFromJsonAsync<FeatureDto>();
        feature.Should().NotBeNull();
        feature!.Id.Should().Be(createdFeature.Id);
        feature.Title.Should().Be("Export to CSV");
    }

    [Fact]
    public async Task GetFeature_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/features/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateFeature_WithValidData_UpdatesFeature()
    {
        // Arrange
        var domainId = await CreateTestDomainAsync();
        var createCommand = new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Original Title",
            Description = "Original Description",
            CreatedBy = Guid.NewGuid()
        };
        var createResponse = await Client.PostAsJsonAsync("/api/features", createCommand);
        var feature = await createResponse.Content.ReadFromJsonAsync<FeatureDto>();

        var updateCommand = new UpdateFeatureCommand
        {
            Id = feature!.Id,
            Title = "Updated Title",
            Description = "Updated Description",
            EstimatedEffortPoints = 8,
            BusinessValueScore = 85.5m
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/features/{feature.Id}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedFeature = await response.Content.ReadFromJsonAsync<FeatureDto>();
        updatedFeature.Should().NotBeNull();
        updatedFeature!.Title.Should().Be("Updated Title");
        updatedFeature.Description.Should().Be("Updated Description");
        updatedFeature.EstimatedEffortPoints.Should().Be(8);
        updatedFeature.BusinessValueScore.Should().Be(85.5m);
    }

    [Fact]
    public async Task DeleteFeature_WithExistingId_DeletesFeature()
    {
        // Arrange
        var domainId = await CreateTestDomainAsync();
        var command = new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Feature to Delete",
            Description = "Will be deleted",
            CreatedBy = Guid.NewGuid()
        };
        var createResponse = await Client.PostAsJsonAsync("/api/features", command);
        var feature = await createResponse.Content.ReadFromJsonAsync<FeatureDto>();

        // Act
        var response = await Client.DeleteAsync($"/api/features/{feature!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"/api/features/{feature.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFeaturesByDomain_ReturnsAllFeaturesInDomain()
    {
        // Arrange
        var domainId = await CreateTestDomainAsync();
        
        await Client.PostAsJsonAsync("/api/features", new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Feature 1",
            Description = "First feature",
            CreatedBy = Guid.NewGuid()
        });

        await Client.PostAsJsonAsync("/api/features", new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Feature 2",
            Description = "Second feature",
            CreatedBy = Guid.NewGuid()
        });

        // Act
        var response = await Client.GetAsync($"/api/features/domain/{domainId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var features = await response.Content.ReadFromJsonAsync<List<FeatureDto>>();
        features.Should().NotBeNull();
        features!.Should().HaveCount(2);
        features.Should().Contain(f => f.Title == "Feature 1");
        features.Should().Contain(f => f.Title == "Feature 2");
    }

    [Fact]
    public async Task GetFeaturesByStatus_ReturnsFilteredFeatures()
    {
        // Arrange
        var domainId = await CreateTestDomainAsync();
        
        var createCommand = new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Proposed Feature",
            Description = "In proposed state",
            CreatedBy = Guid.NewGuid()
        };
        await Client.PostAsJsonAsync("/api/features", createCommand);

        // Act
        var response = await Client.GetAsync($"/api/features/status/{FeatureStatus.Proposed}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var features = await response.Content.ReadFromJsonAsync<List<FeatureDto>>();
        features.Should().NotBeNull();
        features!.Should().HaveCountGreaterThan(0);
        features.Should().OnlyContain(f => f.Status == FeatureStatus.Proposed);
    }

    [Fact]
    public async Task UpdateFeatureStatus_WithValidData_UpdatesStatus()
    {
        // Arrange
        var domainId = await CreateTestDomainAsync();
        var createCommand = new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Feature for Status Update",
            Description = "Test",
            CreatedBy = Guid.NewGuid()
        };
        var createResponse = await Client.PostAsJsonAsync("/api/features", createCommand);
        var feature = await createResponse.Content.ReadFromJsonAsync<FeatureDto>();

        var updateCommand = new UpdateFeatureStatusCommand
        {
            Id = feature!.Id,
            Status = FeatureStatus.InProgress
        };

        // Act
        var response = await Client.PatchAsync($"/api/features/{feature.Id}/status", 
            JsonContent.Create(updateCommand));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedFeature = await response.Content.ReadFromJsonAsync<FeatureDto>();
        updatedFeature.Should().NotBeNull();
        updatedFeature!.Status.Should().Be(FeatureStatus.InProgress);
    }

    [Fact]
    public async Task UpdateFeaturePriority_WithValidData_UpdatesPriority()
    {
        // Arrange
        var domainId = await CreateTestDomainAsync();
        var createCommand = new CreateFeatureCommand
        {
            DomainId = domainId,
            Title = "Feature for Priority Update",
            Description = "Test",
            CreatedBy = Guid.NewGuid()
        };
        var createResponse = await Client.PostAsJsonAsync("/api/features", createCommand);
        var feature = await createResponse.Content.ReadFromJsonAsync<FeatureDto>();

        var updateCommand = new UpdateFeaturePriorityCommand
        {
            Id = feature!.Id,
            PriorityScore = 0.95m,
            Reasoning = "High customer demand and strategic importance"
        };

        // Act
        var response = await Client.PatchAsync($"/api/features/{feature.Id}/priority", 
            JsonContent.Create(updateCommand));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedFeature = await response.Content.ReadFromJsonAsync<FeatureDto>();
        updatedFeature.Should().NotBeNull();
    }
}
