using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Application.Commands.FeatureRequests;
using MediatR;

namespace ProductIntelligence.TeamsBot.Bots;

/// <summary>
/// The Product Intelligence Agent for Microsoft Teams.
/// Uses the shared Azure OpenAI service to analyze channel messages.
/// </summary>
public class ProductIntelligenceAgent : ActivityHandler
{
    private readonly IAzureOpenAIService _aiService;
    private readonly IMediator _mediator;
    private readonly ILogger<ProductIntelligenceAgent> _logger;

    public ProductIntelligenceAgent(
        IAzureOpenAIService aiService,
        IMediator mediator,
        ILogger<ProductIntelligenceAgent> logger)
    {
        _aiService = aiService;
        _mediator = mediator;
        _logger = logger;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var text = turnContext.Activity.Text;

        // 1. Use AI to analyze if this is a feature request
        // In a full implementation, we'd use the Teams AI Library's Planner here.
        // For now, we'll simulate the intelligence by calling our shared AI service.
        _logger.LogInformation("Processing message from Teams: {Text}", text);

        await turnContext.SendActivityAsync(MessageFactory.Text("I'm analyzing your request using Product Intelligence AI..."), cancellationToken);

        try 
        {
            // 2. Submit as a feature request via our shared Application layer
            var command = new SubmitFeatureRequestCommand
            {
                Title = text.Length > 50 ? text.Substring(0, 47) + "..." : text,
                Description = text,
                RequesterName = turnContext.Activity.From.Name,
                RequesterEmail = null, // Teams doesn't always provide email in the activity
                Source = ProductIntelligence.Core.Enums.RequestSource.Teams,
                SourceId = turnContext.Activity.Id
            };

            var result = await _mediator.Send(command, cancellationToken);

            // 3. Respond with the result
            await turnContext.SendActivityAsync(MessageFactory.Text($"✅ I've captured this as a new feature request (ID: {result.Id}). Our AI is now analyzing it for duplicates and priority."), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Teams message");
            await turnContext.SendActivityAsync(MessageFactory.Text("I encountered an error while processing your request. Please try again later."), cancellationToken);
        }
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome to the Product Intelligence channel, {member.Name}! I'm your AI Agent. Post any feature ideas here, and I'll automatically analyze and bucket them for the product team."), cancellationToken);
            }
        }
    }
}
