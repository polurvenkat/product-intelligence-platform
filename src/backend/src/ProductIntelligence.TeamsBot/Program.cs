using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using ProductIntelligence.TeamsBot.Bots;
using ProductIntelligence.Infrastructure.Configuration;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Infrastructure.AI;
using ProductIntelligence.Infrastructure.Data;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// 1. Add Bot Framework services
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
builder.Services.AddTransient<IBot, ProductIntelligenceAgent>();

// 2. Add Shared Infrastructure (Same as API)
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

// 3. Add Repositories
builder.Services.AddScoped<IFeatureRequestRepository, FeatureRequestRepository>();

// 4. Add MediatR (Shared Application Logic)
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(ProductIntelligence.Application.Commands.FeatureRequests.SubmitFeatureRequestCommand).Assembly);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Helper class for Bot Adapter
public class AdapterWithErrorHandler : CloudAdapter
{
    public AdapterWithErrorHandler(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<IBotFrameworkHttpAdapter> logger)
        : base(configuration, httpClientFactory, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");
            await turnContext.SendActivityAsync("The bot encountered an error or bug.");
        };
    }
}
