using ProductIntelligence.Infrastructure.AI;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Infrastructure.Configuration;
using ProductIntelligence.Infrastructure.Data;
using ProductIntelligence.Infrastructure.Repositories;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Workers;
using Azure.Identity;

var builder = Host.CreateApplicationBuilder(args);

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
        
        // Ensure local settings still work in development
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Config] Error adding Key Vault to Workers: {ex.Message}");
    }
}

// Configure Azure OpenAI
builder.Services.Configure<AzureOpenAIOptions>(
    builder.Configuration.GetSection("AzureOpenAI"));

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

var devOpsPat = builder.Configuration["AzureDevOps:PersonalAccessToken"];
if (!string.IsNullOrEmpty(devOpsPat))
{
    builder.Services.Configure<AzureOpenAIOptions>(options => { }); // ensure section is initialized
    builder.Services.Configure<AzureDevOpsOptions>(options => options.PersonalAccessToken = devOpsPat);
}

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

// Register infrastructure services
builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();

// Register repositories
builder.Services.AddScoped<IFeatureRequestRepository, FeatureRequestRepository>();
builder.Services.AddScoped<IFeatureRepository, FeatureRepository>();
builder.Services.AddScoped<IDomainRepository, DomainRepository>();

// Add background services
builder.Services.AddHostedService<FeatureRequestProcessorWorker>();
builder.Services.AddHostedService<PriorityCalculationWorker>();
builder.Services.AddHostedService<EmbeddingGeneratorWorker>();
builder.Services.AddHostedService<DocumentProcessorWorker>();

var host = builder.Build();

// Log startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Product Intelligence Background Workers starting...");
logger.LogInformation("Registered workers: FeatureRequestProcessor, PriorityCalculation, EmbeddingGenerator, DocumentProcessor");

host.Run();
