using FluentMigrator.Runner;
using FluentValidation;
using ProductIntelligence.Infrastructure.Configuration;
using ProductIntelligence.Infrastructure.Data;
using ProductIntelligence.Infrastructure.Repositories;
using ProductIntelligence.Infrastructure.AI;
using ProductIntelligence.Core.Interfaces.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.Configure<AzureAISearchOptions>(builder.Configuration.GetSection("AzureAISearch"));
builder.Services.Configure<AzureBlobStorageOptions>(builder.Configuration.GetSection("AzureBlobStorage"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));

// FluentMigrator
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(ProductIntelligence.Infrastructure.Data.Migrations.InitialSchema).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// Repositories
builder.Services.AddScoped<IDomainRepository, DomainRepository>();
builder.Services.AddScoped<IFeatureRequestRepository, FeatureRequestRepository>();

// Azure Services
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();

// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(ProductIntelligence.Application.Commands.Domains.CreateDomainCommand).Assembly);
});

// Validation
builder.Services.AddValidatorsFromAssembly(typeof(ProductIntelligence.Application.Commands.Domains.CreateDomainCommand).Assembly);

// Controllers
builder.Services.AddControllers();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
