using FluentMigrator.Runner;
using FluentValidation;
using ProductIntelligence.Infrastructure.Configuration;
using ProductIntelligence.Infrastructure.Data;
using ProductIntelligence.Infrastructure.Repositories;
using ProductIntelligence.Infrastructure.AI;
using ProductIntelligence.Application.Interfaces.AI;
using ProductIntelligence.Application.Interfaces.AzureDevOps;
using ProductIntelligence.Infrastructure.Services.AzureDevOps;
using ProductIntelligence.Core.Interfaces.Repositories;
using ProductIntelligence.API.Middleware;
using ProductIntelligence.Application.Interfaces;
using ProductIntelligence.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Dapper;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Enable legacy timestamp behavior for Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Azure Key Vault Configuration
var keyVaultUrl = builder.Configuration["KeyVault:Url"] ?? "https://farmerapp-configuration.vault.azure.net/";
Console.WriteLine($"[Config] Attempting to use Key Vault: {keyVaultUrl}");

if (!string.IsNullOrEmpty(keyVaultUrl))
{
    try 
    {
        var managedIdentityClientId = builder.Configuration["Azure:ManagedIdentityClientId"];
        var credentialOptions = new DefaultAzureCredentialOptions();
        
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
            Console.WriteLine($"[Config] Using User-Assigned Managed Identity Client ID: {managedIdentityClientId}");
        }

        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential(credentialOptions));
        Console.WriteLine("[Config] Azure Key Vault configuration provider added.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Config] Error adding Key Vault: {ex.Message}");
    }
}

// Configuration
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.Configure<AzureAISearchOptions>(builder.Configuration.GetSection("AzureAISearch"));
builder.Services.Configure<AzureBlobStorageOptions>(builder.Configuration.GetSection("AzureBlobStorage"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<AzureDevOpsOptions>(builder.Configuration.GetSection("AzureDevOps"));

// Database
var kvConnectionString = builder.Configuration.GetConnectionString("ProductIntelligencePlatformDb");
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var connectionString = kvConnectionString ?? defaultConnectionString
    ?? throw new InvalidOperationException("Connection string 'ProductIntelligencePlatformDb' or 'DefaultConnection' not found");

if (!string.IsNullOrEmpty(kvConnectionString))
{
    Console.WriteLine("[Config] Using connection string from Key Vault (ProductIntelligencePlatformDb).");
}
else
{
    Console.WriteLine("[Config] Falling back to DefaultConnection from appsettings.");
}

// Mask password for logging
var maskedConnectionString = System.Text.RegularExpressions.Regex.Replace(connectionString, @"Password=[^;]+", "Password=****");
Console.WriteLine($"[Config] Connection String: {maskedConnectionString}");

builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));

// Configure Dapper to map snake_case columns to PascalCase properties
DefaultTypeMap.MatchNamesWithUnderscores = true;

// FluentMigrator
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(ProductIntelligence.Infrastructure.Data.Migrations.InitialSchema).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<IFeatureRepository, FeatureRepository>();
builder.Services.AddScoped<IFeatureRequestRepository, FeatureRequestRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IFeatureVoteRepository, FeatureVoteRepository>();
builder.Services.AddScoped<IDomainGoalRepository, DomainGoalRepository>();
builder.Services.AddScoped<IRoadmapRepository, RoadmapRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkItemManager, WorkItemManager>();
builder.Services.AddScoped<IRoadmapManager, RoadmapManager>();

// Azure Services
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddHttpClient<IAzureDevOpsService, AzureDevOpsService>();

// AI Agents
builder.Services.AddScoped<IFeatureDeduplicationAgent, FeatureDeduplicationAgent>();

// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(ProductIntelligence.Application.Commands.Domains.CreateDomainCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ProductIntelligence.Infrastructure.Repositories.DomainRepository).Assembly);
    cfg.AddOpenBehavior(typeof(ProductIntelligence.Application.Behaviors.ValidationBehavior<,>));
});

// Validation
builder.Services.AddValidatorsFromAssembly(typeof(ProductIntelligence.Application.Commands.Domains.CreateDomainCommand).Assembly);

// Controllers
builder.Services.AddControllers();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProductIntelligence";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ProductIntelligenceApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Product Intelligence API", Version = "v1" });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Flutter web dev
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

var app = builder.Build();

// Run migrations
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

// Configure pipeline
// Swagger must be early in the pipeline to serve static files
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Intelligence API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root (http://localhost:5000)
    });
}
else 
{
    app.UseHttpsRedirection();
}

// Routing must come before CORS, Auth, and custom middleware
app.UseRouting();

app.UseCors();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Global exception handler (after routing but before authorization)
app.UseGlobalExceptionHandler();

// Request logging
app.UseRequestLogging();

// Map endpoints
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
