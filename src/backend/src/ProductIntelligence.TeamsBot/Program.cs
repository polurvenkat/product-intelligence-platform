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
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Azure Key Vault Configuration
var keyVaultUrl = builder.Configuration["KeyVault:Url"] ?? "https://farmerapp-configuration.vault.azure.net/";
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    try 
    {
        var managedIdentityClientId = builder.Configuration["Azure:ManagedIdentityClientId"];
        var credentialOptions = new DefaultAzureCredentialOptions();
        
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
        }

        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential(credentialOptions));
        
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Config] Error adding Key Vault to TeamsBot: {ex.Message}");
    }
}

// 1. Add Bot Framework services
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
builder.Services.AddTransient<IBot, ProductIntelligenceAgent>();

// 2. Add Shared Infrastructure (Same as API)
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));

// Explicitly handle sensitive keys from Key Vault that might not bind correctly to sections
var aiKey = builder.Configuration["AzureOpenAI:ApiKey"];
var aiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
if (!string.IsNullOrEmpty(aiKey) || !string.IsNullOrEmpty(aiEndpoint))
{
    builder.Services.Configure<AzureOpenAIOptions>(options => 
    {
        if (!string.IsNullOrEmpty(aiKey)) options.ApiKey = aiKey;
        if (!string.IsNullOrEmpty(aiEndpoint)) options.Endpoint = aiEndpoint;
    });
}

builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();

// Database connection
var kvConnectionString = builder.Configuration.GetConnectionString("ProductIntelligencePlatformDb");
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var connectionString = builder.Environment.IsDevelopment() 
    ? (defaultConnectionString ?? kvConnectionString)
    : (kvConnectionString ?? defaultConnectionString);

if (connectionString == null)
{
    throw new InvalidOperationException("Connection string 'ProductIntelligencePlatformDb' or 'DefaultConnection' not found");
}

builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));

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
