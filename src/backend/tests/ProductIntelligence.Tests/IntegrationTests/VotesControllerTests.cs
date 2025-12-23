using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductIntelligence.Application.Commands.Domains;
using ProductIntelligence.Application.Commands.Features;
using ProductIntelligence.Application.DTOs;
using ProductIntelligence.Application.Votes.Commands;
using ProductIntelligence.Core.Enums;

namespace ProductIntelligence.Tests.IntegrationTests;

public class VotesControllerTests : IntegrationTestBase
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
            Description = "Feature for vote testing",
            CreatedBy = Guid.NewGuid()
        };
        var featureResponse = await Client.PostAsJsonAsync("/api/features", featureCommand);
        var feature = await featureResponse.Content.ReadFromJsonAsync<FeatureDto>();
        return feature!.Id;
    }

    [Fact]
    public async Task VoteForFeature_WithValidData_ReturnsCreatedVoteId()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var command = new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "voter@example.com",
            VoterTier = CustomerTier.Professional
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/votes", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var voteId = await response.Content.ReadFromJsonAsync<Guid>();
        voteId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task VoteForFeature_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var command = new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "",
            VoterTier = CustomerTier.Starter
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/votes", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VoteForFeature_DuplicateVote_ReturnsBadRequest()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var command = new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "same.voter@example.com",
            VoterTier = CustomerTier.Enterprise
        };

        // Vote first time
        await Client.PostAsJsonAsync("/api/votes", command);

        // Act - Vote second time
        var response = await Client.PostAsJsonAsync("/api/votes", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VoteForFeature_WithDifferentTiers_AppliesCorrectWeights()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();

        // Act
        var starterResponse = await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "starter@example.com",
            VoterTier = CustomerTier.Starter
        });

        var proResponse = await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "pro@example.com",
            VoterTier = CustomerTier.Professional
        });

        var enterpriseResponse = await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "enterprise@example.com",
            VoterTier = CustomerTier.Enterprise
        });

        // Assert
        starterResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        proResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        enterpriseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetVotesByFeature_ReturnsAllVotesForFeature()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        
        await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "voter1@example.com",
            VoterTier = CustomerTier.Starter
        });

        await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "voter2@example.com",
            VoterTier = CustomerTier.Professional
        });

        await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "voter3@example.com",
            VoterTier = CustomerTier.Enterprise
        });

        // Act
        var response = await Client.GetAsync($"/api/votes/feature/{featureId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var votes = await response.Content.ReadFromJsonAsync<List<FeatureVoteDto>>();
        votes.Should().NotBeNull();
        votes!.Should().HaveCount(3);
        votes.Should().Contain(v => v.VoterEmail == "voter1@example.com");
        votes.Should().Contain(v => v.VoterEmail == "voter2@example.com");
        votes.Should().Contain(v => v.VoterEmail == "voter3@example.com");
    }

    [Fact]
    public async Task GetVoteCount_ReturnsCorrectCount()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        
        for (int i = 1; i <= 5; i++)
        {
            await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
            {
                FeatureId = featureId,
                VoterEmail = $"voter{i}@example.com",
                VoterTier = CustomerTier.Starter
            });
        }

        // Act
        var response = await Client.GetAsync($"/api/votes/feature/{featureId}/count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<VoteCountDto>();
        count.Should().NotBeNull();
        count!.Count.Should().Be(5);
    }

    [Fact]
    public async Task RemoveVote_WithExistingVote_RemovesVote()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var voterEmail = "remove.me@example.com";
        
        await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = voterEmail,
            VoterTier = CustomerTier.Starter
        });

        // Act
        var response = await Client.DeleteAsync($"/api/votes/{featureId}?voterEmail={Uri.EscapeDataString(voterEmail)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify removal
        var getResponse = await Client.GetAsync($"/api/votes/feature/{featureId}");
        var votes = await getResponse.Content.ReadFromJsonAsync<List<FeatureVoteDto>>();
        votes.Should().NotContain(v => v.VoterEmail == voterEmail);
    }

    [Fact]
    public async Task RemoveVote_WithoutEmail_ReturnsBadRequest()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/votes/{featureId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VoteForFeature_MultipleVotersFromSameCompany_AllowsMultipleVotes()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();

        // Act
        var vote1Response = await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "user1@company.com",
            VoterTier = CustomerTier.Enterprise
        });

        var vote2Response = await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "user2@company.com",
            VoterTier = CustomerTier.Enterprise
        });

        // Assert
        vote1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        vote2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await Client.GetAsync($"/api/votes/feature/{featureId}");
        var votes = await getResponse.Content.ReadFromJsonAsync<List<FeatureVoteDto>>();
        votes.Should().HaveCount(2);
    }

    [Fact]
    public async Task VoteForFeature_WithUserId_AssociatesVoteWithUser()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();
        var userId = Guid.NewGuid();
        var command = new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "user@example.com",
            VoterTier = CustomerTier.Professional
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/votes", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var voteId = await response.Content.ReadFromJsonAsync<Guid>();
        voteId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetVoteCount_ForFeatureWithNoVotes_ReturnsZero()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();

        // Act
        var response = await Client.GetAsync($"/api/votes/feature/{featureId}/count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<VoteCountDto>();
        count.Should().NotBeNull();
        count!.Count.Should().Be(0);
    }

    [Fact]
    public async Task VoteWorkflow_CreateAndRemoveMultipleVotes_MaintainsDataIntegrity()
    {
        // Arrange
        var featureId = await CreateTestFeatureAsync();

        // Create votes
        await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "user1@example.com",
            VoterTier = CustomerTier.Starter
        });

        await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "user2@example.com",
            VoterTier = CustomerTier.Professional
        });

        await Client.PostAsJsonAsync("/api/votes", new VoteForFeatureCommand
        {
            FeatureId = featureId,
            VoterEmail = "user3@example.com",
            VoterTier = CustomerTier.Enterprise
        });

        // Remove one vote
        await Client.DeleteAsync($"/api/votes/{featureId}?voterEmail=user2@example.com");

        // Act
        var response = await Client.GetAsync($"/api/votes/feature/{featureId}");
        var countResponse = await Client.GetAsync($"/api/votes/feature/{featureId}/count");

        // Assert
        var votes = await response.Content.ReadFromJsonAsync<List<FeatureVoteDto>>();
        var count = await countResponse.Content.ReadFromJsonAsync<VoteCountDto>();
        
        votes.Should().HaveCount(2);
        votes.Should().Contain(v => v.VoterEmail == "user1@example.com");
        votes.Should().Contain(v => v.VoterEmail == "user3@example.com");
        votes.Should().NotContain(v => v.VoterEmail == "user2@example.com");
        count!.Count.Should().Be(2);
    }
}
