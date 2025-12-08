using ProductIntelligence.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Add background services
builder.Services.AddHostedService<DocumentProcessorWorker>();

var host = builder.Build();
host.Run();
