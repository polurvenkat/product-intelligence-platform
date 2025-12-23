using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductIntelligence.Application.Commands.Domains;
using ProductIntelligence.Application.Commands.Features;
using ProductIntelligence.Application.Commands.FeatureRequests;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Feedback.Commands;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Tests.IntegrationTests;

public class FeedbackControllerTests : IntegrationTestBase
{
    private async Task<Guid> CreateTestFeatureAsync()
    {
        var domainCommand = new CreateDomainCommand
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test Domain",
            Description = "Test"
        };
        var domainResponse = await Client.PostAsJsonAsync("/api/domains", domainCommand);
        var domain = await domainResponse.Content.ReadFromJsonAsync<DomainDto>();

        var featureCommand = new CreateFeatureCommand
        {
            DomainId = domain!.Id,
            Title = "Test Feature",
            Description = "Feature for feedback testing",
            CreatedBy = Guid.NewGuid()
        };
        var featureResponse = await Client.PostAsJsonAsync("/api/features", featureCommand);
        var feature = await featureResponse.Content.ReadFromJsonAsync<FeatureDto>();
        return feature!.Id;
    }

    private async Task<Guid> CreateTestFeatureRequestAsync()
    {
        var command = new SubmitFeatureRequestCommand
        {
            Title = "Test Feature Request",
            Description = "Request for feedback testing",
            RequesterName = "Test User"
        };
        var response = await Client.PostAsJsonAsync("/api/feature-requests", command);
        var request = await response.Content.ReadFromJsonAsync<FeatureRequestDto>();
        return request!.Id;
    }

    [Fact]
    public async Task SubmitFeedback_ForFeature_ReturnsCreatedFeedbackId()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var command = new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "This feature is great! Love the dark mode implementation.",
            Source = RequestSource.API,
            CustomerId = "CUST-12345",
            CustomerTier = CustomerTier.Enterprise
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedback", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var feedbackId = await response.Content.ReadFromJsonAsync<Guid>();
        feedbackId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SubmitFeedback_ForFeatureRequest_ReturnsCreatedFeedbackId()
    {
        // Arrange
        var requestId = await CreateTestFeatureRequestAsync();
        var command = new SubmitFeedbackCommand
        {
            FeatureRequestId = requestId,
            Content = "I really need this feature for our business operations.",
            Source = RequestSource.Email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedback", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var feedbackId = await response.Content.ReadFromJsonAsync<Guid>();
        feedbackId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SubmitFeedback_WithEmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var command = new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "",
            Source = RequestSource.API
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedback", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitFeedback_WithNeitherFeatureNorRequest_ReturnsBadRequest()
    {
        // Arrange
        var command = new SubmitFeedbackCommand
        {
            Content = "This is feedback without a target",
            Source = RequestSource.API
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedback", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeedback_WithExistingId_ReturnsFeedback()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var command = new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "Feedback to retrieve",
            Source = RequestSource.API
        };
        var createResponse = await Client.PostAsJsonAsync("/api/feedback", command);
        var feedbackId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var response = await Client.GetAsync($"/api/feedback/{feedbackId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var feedback = await response.Content.ReadFromJsonAsync<FeedbackDto>();
        feedback.Should().NotBeNull();
        feedback!.Id.Should().Be(feedbackId);
        feedback.Content.Should().Be("Feedback to retrieve");
        feedback.Sentiment.Should().Be(Sentiment.Positive);
    }

    [Fact]
    public async Task GetFeedback_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/feedback/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFeedbackByFeature_ReturnsAllFeedbackForFeature()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        
        await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "First feedback",
            Source = RequestSource.API
        });

        await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "Second feedback",
            Source = RequestSource.Email
        });

        // Act
        var response = await Client.GetAsync($"/api/feedback/feature/{featureId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var feedbackList = await response.Content.ReadFromJsonAsync<List<FeedbackDto>>();
        feedbackList.Should().NotBeNull();
        feedbackList!.Should().HaveCount(2);
        feedbackList.Should().Contain(f => f.Content == "First feedback");
        feedbackList.Should().Contain(f => f.Content == "Second feedback");
    }

    [Fact]
    public async Task GetFeedbackByRequest_ReturnsAllFeedbackForRequest()
    {
        // Arrange
        var requestId = await CreateTestFeatureRequestAsync();
        
        await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureRequestId = requestId,
            Content = "Request feedback 1",
            Source = RequestSource.API
        });

        await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureRequestId = requestId,
            Content = "Request feedback 2",
            Source = RequestSource.Email
        });

        // Act
        var response = await Client.GetAsync($"/api/feedback/request/{requestId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var feedbackList = await response.Content.ReadFromJsonAsync<List<FeedbackDto>>();
        feedbackList.Should().NotBeNull();
        feedbackList!.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitFeedback_WithDifferentSentiments_StoresCorrectly()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();

        // Act
        var positiveResponse = await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "Love it!",
            Source = RequestSource.API
        });

        var negativeResponse = await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "Needs improvement",
            Source = RequestSource.Email
        });

        var neutralResponse = await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "It's okay",
            Source = RequestSource.Slack
        });

        // Assert
        positiveResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        negativeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        neutralResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var allFeedbackResponse = await Client.GetAsync($"/api/feedback/feature/{featureId}");
        var allFeedback = await allFeedbackResponse.Content.ReadFromJsonAsync<List<FeedbackDto>>();
        
        allFeedback.Should().Contain(f => f.Sentiment == Sentiment.Positive);
        allFeedback.Should().Contain(f => f.Sentiment == Sentiment.Negative);
        allFeedback.Should().Contain(f => f.Sentiment == Sentiment.Neutral);
    }

    [Fact]
    public async Task SubmitFeedback_WithCustomerInformation_StoresCustomerData()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var command = new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "Feedback from enterprise customer",
            CustomerId = "CUST-ENT-001",
            CustomerTier = CustomerTier.Enterprise,
            Source = RequestSource.API
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedback", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var feedbackId = await response.Content.ReadFromJsonAsync<Guid>();

        var getFeedbackResponse = await Client.GetAsync($"/api/feedback/{feedbackId}");
        var feedback = await getFeedbackResponse.Content.ReadFromJsonAsync<FeedbackDto>();
        
        feedback.Should().NotBeNull();
        feedback!.CustomerTier.Should().Be(CustomerTier.Enterprise);
        feedback.Source.Should().Be(RequestSource.API);
    }

    [Fact]
    public async Task SubmitMultipleFeedback_FromDifferentSources_TracksSourceCorrectly()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();

        // Act
        await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "From email",
            Source = RequestSource.Email
        });

        await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "From API",
            Source = RequestSource.API
        });

        await Client.PostAsJsonAsync("/api/feedback", new SubmitFeedbackCommand
        {
            FeatureId = featureId,
            Content = "From Slack",
            Source = RequestSource.Slack
        });

        // Assert
        var response = await Client.GetAsync($"/api/feedback/feature/{featureId}");
        var feedbackList = await response.Content.ReadFromJsonAsync<List<FeedbackDto>>();
        
        feedbackList.Should().NotBeNull();
        feedbackList!.Should().HaveCount(3);
        feedbackList.Should().Contain(f => f.Source == RequestSource.Email);
        feedbackList.Should().Contain(f => f.Source == RequestSource.API);
        feedbackList.Should().Contain(f => f.Source == RequestSource.Slack);
    }
}
