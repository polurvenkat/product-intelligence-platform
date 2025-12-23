using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductIntelligence.Application.Commands.FeatureRequests;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Tests.IntegrationTests;

public class FeatureRequestsControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task SubmitFeatureRequest_WithValidData_ReturnsCreatedRequest()
    {
        // Arrange
        var command = new SubmitFeatureRequestCommand
        {
            Title = "Add Multi-Factor Authentication",
            Description = "We need MFA support for enhanced security",
            RequesterName = "John Doe",
            RequesterEmail = "john.doe@example.com",
            RequesterCompany = "Acme Corp",
            RequesterTier = CustomerTier.Enterprise,
            Source = RequestSource.Email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feature-requests", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var request = await response.Content.ReadFromJsonAsync<FeatureRequestDto>();
        request.Should().NotBeNull();
        request!.Id.Should().NotBeEmpty();
        request.Title.Should().Be("Add Multi-Factor Authentication");
        request.Description.Should().Be("We need MFA support for enhanced security");
        request.RequesterName.Should().Be("John Doe");
        request.RequesterEmail.Should().Be("john.doe@example.com");
        request.RequesterTier.Should().Be(CustomerTier.Enterprise);
        request.Status.Should().Be(RequestStatus.Pending);
        request.Source.Should().Be(RequestSource.Email);
    }

    [Fact]
    public async Task SubmitFeatureRequest_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var command = new SubmitFeatureRequestCommand
        {
            Title = "",
            Description = "Some description",
            RequesterName = "John Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feature-requests", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitFeatureRequest_WithAllFields_StoresAllData()
    {
        // Arrange
        var command = new SubmitFeatureRequestCommand
        {
            Title = "API Rate Limiting",
            Description = "Implement rate limiting on all public APIs to prevent abuse",
            RequesterName = "Jane Smith",
            RequesterEmail = "jane.smith@enterprise.com",
            RequesterCompany = "Enterprise Solutions Inc",
            RequesterTier = CustomerTier.Professional,
            Source = RequestSource.API,
            SourceId = "PORTAL-12345"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feature-requests", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var request = await response.Content.ReadFromJsonAsync<FeatureRequestDto>();
        request.Should().NotBeNull();
        request!.SourceId.Should().Be("PORTAL-12345");
        request.RequesterCompany.Should().Be("Enterprise Solutions Inc");
    }

    [Fact]
    public async Task GetPendingRequests_ReturnsAllPendingRequests()
    {
        // Arrange
        await Client.PostAsJsonAsync("/api/feature-requests", new SubmitFeatureRequestCommand
        {
            Title = "Pending Request 1",
            Description = "First pending request",
            RequesterName = "User 1"
        });

        await Client.PostAsJsonAsync("/api/feature-requests", new SubmitFeatureRequestCommand
        {
            Title = "Pending Request 2",
            Description = "Second pending request",
            RequesterName = "User 2"
        });

        // Act
        var response = await Client.GetAsync("/api/feature-requests/pending?limit=100&offset=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<FeatureRequestDto>>();
        requests.Should().NotBeNull();
        requests!.Should().HaveCountGreaterThanOrEqualTo(2);
        requests.Should().Contain(r => r.Title == "Pending Request 1");
        requests.Should().Contain(r => r.Title == "Pending Request 2");
        requests.Should().OnlyContain(r => r.Status == RequestStatus.Pending);
    }

    [Fact]
    public async Task GetPendingRequests_WithLimitParameter_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            await Client.PostAsJsonAsync("/api/feature-requests", new SubmitFeatureRequestCommand
            {
                Title = $"Request {i}",
                Description = $"Description {i}",
                RequesterName = "Test User"
            });
        }

        // Act
        var response = await Client.GetAsync("/api/feature-requests/pending?limit=10&offset=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<FeatureRequestDto>>();
        requests.Should().NotBeNull();
        requests!.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetRequestsByStatus_ReturnsFilteredRequests()
    {
        // Arrange
        await Client.PostAsJsonAsync("/api/feature-requests", new SubmitFeatureRequestCommand
        {
            Title = "Pending Request",
            Description = "Will be pending",
            RequesterName = "User 1"
        });

        // Act
        var response = await Client.GetAsync($"/api/feature-requests/status/{RequestStatus.Pending}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<FeatureRequestDto>>();
        requests.Should().NotBeNull();
        requests!.Should().OnlyContain(r => r.Status == RequestStatus.Pending);
    }

    [Fact]
    public async Task GetRequestsByFeature_ReturnsLinkedRequests()
    {
        // Arrange
        var featureId = Guid.NewGuid();
        
        // Create domain first
        var domainId = Guid.NewGuid();
        await Client.PostAsJsonAsync("/api/domains", new { 
            OrganizationId = Guid.NewGuid(),
            Name = "Test Domain",
            Description = "Test"
        });

        // Act
        var response = await Client.GetAsync($"/api/feature-requests/feature/{featureId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<FeatureRequestDto>>();
        requests.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRequestStatus_WithValidData_UpdatesStatus()
    {
        // Arrange
        var createCommand = new SubmitFeatureRequestCommand
        {
            Title = "Request for Status Update",
            Description = "Will update status",
            RequesterName = "Test User"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/feature-requests", createCommand);
        var request = await createResponse.Content.ReadFromJsonAsync<FeatureRequestDto>();

        var updateCommand = new UpdateRequestStatusCommand
        {
            RequestId = request!.Id,
            Status = RequestStatus.Reviewing
        };

        // Act
        var response = await Client.PatchAsync($"/api/feature-requests/{request.Id}/status", 
            JsonContent.Create(updateCommand));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateRequestStatus_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new UpdateRequestStatusCommand
        {
            RequestId = nonExistentId,
            Status = RequestStatus.Accepted
        };

        // Act
        var response = await Client.PatchAsync($"/api/feature-requests/{nonExistentId}/status", 
            JsonContent.Create(command));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkRequestAsDuplicate_WithValidData_MarksDuplicate()
    {
        // Arrange
        var original = new SubmitFeatureRequestCommand
        {
            Title = "Original Request",
            Description = "The original",
            RequesterName = "User 1"
        };
        var originalResponse = await Client.PostAsJsonAsync("/api/feature-requests", original);
        var originalRequest = await originalResponse.Content.ReadFromJsonAsync<FeatureRequestDto>();

        var duplicate = new SubmitFeatureRequestCommand
        {
            Title = "Duplicate Request",
            Description = "Same as original",
            RequesterName = "User 2"
        };
        var duplicateResponse = await Client.PostAsJsonAsync("/api/feature-requests", duplicate);
        var duplicateRequest = await duplicateResponse.Content.ReadFromJsonAsync<FeatureRequestDto>();

        var markCommand = new MarkRequestAsDuplicateCommand
        {
            RequestId = duplicateRequest!.Id,
            DuplicateOfRequestId = originalRequest!.Id,
            SimilarityScore = 0.92m
        };

        // Act
        var response = await Client.PostAsync($"/api/feature-requests/{duplicateRequest.Id}/mark-duplicate", 
            JsonContent.Create(markCommand));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetDuplicateRequests_ReturnsAllDuplicates()
    {
        // Arrange
        var original = new SubmitFeatureRequestCommand
        {
            Title = "Original for Duplicates",
            Description = "The original request",
            RequesterName = "User 1"
        };
        var originalResponse = await Client.PostAsJsonAsync("/api/feature-requests", original);
        var originalRequest = await originalResponse.Content.ReadFromJsonAsync<FeatureRequestDto>();

        // Act
        var response = await Client.GetAsync($"/api/feature-requests/{originalRequest!.Id}/duplicates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var duplicates = await response.Content.ReadFromJsonAsync<List<FeatureRequestDto>>();
        duplicates.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitMultipleRequests_FromDifferentTiers_StoresCorrectly()
    {
        // Arrange & Act
        var starterResponse = await Client.PostAsJsonAsync("/api/feature-requests", new SubmitFeatureRequestCommand
        {
            Title = "Starter Request",
            Description = "From starter tier",
            RequesterName = "Starter User",
            RequesterTier = CustomerTier.Starter
        });

        var proResponse = await Client.PostAsJsonAsync("/api/feature-requests", new SubmitFeatureRequestCommand
        {
            Title = "Professional Request",
            Description = "From professional tier",
            RequesterName = "Pro User",
            RequesterTier = CustomerTier.Professional
        });

        var enterpriseResponse = await Client.PostAsJsonAsync("/api/feature-requests", new SubmitFeatureRequestCommand
        {
            Title = "Enterprise Request",
            Description = "From enterprise tier",
            RequesterName = "Enterprise User",
            RequesterTier = CustomerTier.Enterprise
        });

        // Assert
        starterResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        proResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        enterpriseResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var starterRequest = await starterResponse.Content.ReadFromJsonAsync<FeatureRequestDto>();
        var proRequest = await proResponse.Content.ReadFromJsonAsync<FeatureRequestDto>();
        var enterpriseRequest = await enterpriseResponse.Content.ReadFromJsonAsync<FeatureRequestDto>();

        starterRequest!.RequesterTier.Should().Be(CustomerTier.Starter);
        proRequest!.RequesterTier.Should().Be(CustomerTier.Professional);
        enterpriseRequest!.RequesterTier.Should().Be(CustomerTier.Enterprise);
    }
}
