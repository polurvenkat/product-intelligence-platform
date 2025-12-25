using ProductIntelligence.Infrastructure.AI;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Infrastructure.Configuration;
using ProductIntelligence.Infrastructure.Data;
using ProductIntelligence.Infrastructure.Repositories;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Configure Azure OpenAI
builder.Services.Configure<AzureOpenAIOptions>(
    builder.Configuration.GetSection("AzureOpenAI"));

// Database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

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
