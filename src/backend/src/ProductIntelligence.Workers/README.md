# Product Intelligence Background Workers

This project contains background workers that handle asynchronous AI processing for the Product Intelligence Platform.

## Workers

### 1. FeatureRequestProcessorWorker
**Purpose**: Processes new feature requests to generate embeddings for semantic search and deduplication.

**How it works**:
- Polls every 30 seconds for feature requests without embeddings
- Generates embeddings using Azure OpenAI's text-embedding-3-large model
- Updates requests with embedding vectors for vector similarity search
- Processes up to 10 requests per batch

**Configuration**:
```json
"Workers": {
  "FeatureRequestProcessor": {
    "IntervalSeconds": 30,
    "BatchSize": 10
  }
}
```

### 2. PriorityCalculationWorker
**Purpose**: Periodically recalculates AI priority scores for active features based on latest data.

**How it works**:
- Runs every 6 hours by default
- Analyzes features that are Accepted or InProgress
- Uses GPT-4o to calculate priority scores (0.00 to 1.00) considering:
  - Business value
  - Estimated effort
  - Customer impact
  - Strategic alignment
- Only updates if score changes significantly (>0.1 difference)
- Processes up to 20 features per batch

**Configuration**:
```json
"Workers": {
  "PriorityCalculation": {
    "IntervalHours": 6,
    "BatchSize": 20
  }
}
```

### 3. EmbeddingGeneratorWorker
**Purpose**: Backfills embeddings for features that don't have them.

**How it works**:
- Runs every 5 minutes
- Generates embeddings from feature Title + Description
- Processes up to 15 features per batch
- Note: Feature entity currently doesn't store embeddings - this is prepared for future enhancement

**Configuration**:
```json
"Workers": {
  "EmbeddingGenerator": {
    "IntervalMinutes": 5,
    "BatchSize": 15
  }
}
```

### 4. DocumentProcessorWorker
**Purpose**: Placeholder for future document processing capabilities.

**Planned features**:
- Parse uploaded documents (PDF, Word, etc.)
- Extract content and generate embeddings
- Index in vector store for semantic search
- Link entities to features and domains

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=product_intelligence;Username=postgres;Password=your-password"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-3-large",
    "Temperature": 0.1,
    "MaxTokens": 2000
  }
}
```

### User Secrets (Development)

For development, use user secrets to store sensitive information:

```bash
cd src/ProductIntelligence.Workers
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;..."
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
```

## Running the Workers

### Development

```bash
cd src/ProductIntelligence.Workers
dotnet run
```

### Production

```bash
cd src/ProductIntelligence.Workers
dotnet publish -c Release -o ./publish
cd ./publish
./ProductIntelligence.Workers
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "ProductIntelligence.Workers.dll"]
```

## Logging

The workers use Microsoft.Extensions.Logging with the following log levels:

- **Information**: Startup, shutdown, batch processing summaries
- **Debug**: Individual item processing (can be verbose)
- **Warning**: Non-critical issues (e.g., parsing failures)
- **Error**: Processing failures with stack traces

Configure in appsettings.json:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ProductIntelligence.Workers": "Information"
    }
  }
}
```

## Architecture

The workers follow Clean Architecture principles:

- **Workers Layer**: Background service implementations
- **Application Layer**: Commands, queries, and handlers
- **Infrastructure Layer**: Repositories, AI services, database access
- **Core Layer**: Entities, interfaces, enums

Dependencies flow inward: Workers → Application → Core

## Monitoring

Consider adding:

- **Application Insights**: Distributed tracing and metrics
- **Health Checks**: HTTP endpoint for container health
- **Metrics**: Processing counts, error rates, duration
- **Alerts**: Failed processing, high error rates

## Scaling

Workers can be scaled horizontally:

1. **Database locking**: Implement optimistic concurrency or distributed locks
2. **Message queue**: Replace polling with Azure Service Bus or RabbitMQ
3. **Multiple instances**: Run multiple worker containers
4. **Selective workers**: Deploy workers separately for better resource allocation

## Future Enhancements

- [ ] Add retry logic with exponential backoff
- [ ] Implement circuit breaker for AI service calls
- [ ] Add health check endpoints
- [ ] Integrate Application Insights
- [ ] Add configurable intervals via appsettings
- [ ] Implement message queue instead of polling
- [ ] Add distributed locking for multi-instance deployments
- [ ] Store feature embeddings in database
- [ ] Implement document processing pipeline
