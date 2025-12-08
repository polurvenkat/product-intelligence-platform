# Product Intelligence Platform - Backend

Enterprise-grade product intelligence platform built with .NET 9, PostgreSQL, and Azure AI services.

## 🏗️ Architecture

- **Clean Architecture** with clear separation of concerns
- **CQRS** pattern using MediatR
- **Dapper** for high-performance data access
- **PostgreSQL** with vector extensions for AI-powered features
- **Azure OpenAI** for intelligent feature analysis

## 📁 Project Structure

```
src/
├── ProductIntelligence.API/          # Web API entry point
├── ProductIntelligence.Application/  # Use cases, commands, queries
├── ProductIntelligence.Core/         # Domain entities, interfaces
└── ProductIntelligence.Infrastructure/ # Data access, external services

tests/
└── ProductIntelligence.UnitTests/    # Unit tests
```

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/) with extensions:
  - `uuid-ossp`
  - `ltree`
  - `vector` (pgvector)
- [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service) (optional for local dev)

### Database Setup

```bash
# Install PostgreSQL extensions
psql -U postgres -d product_intelligence -c "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";"
psql -U postgres -d product_intelligence -c "CREATE EXTENSION IF NOT EXISTS \"ltree\";"
psql -U postgres -d product_intelligence -c "CREATE EXTENSION IF NOT EXISTS \"vector\";"
```

### Running the API

```bash
cd src/ProductIntelligence.API
dotnet restore
dotnet run
```

API will be available at `https://localhost:5001`

Swagger UI: `https://localhost:5001/swagger`

### Configuration

Update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=product_intelligence;Username=postgres;Password=yourpassword"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-3-large"
  }
}
```

### Running Tests

```bash
dotnet test
```

## 🔑 Key Features

### Hierarchical Domain Management
- Unlimited nesting with PostgreSQL ltree
- Efficient path queries for domain hierarchies

### AI-Powered Feature Deduplication
- Semantic similarity search using vector embeddings
- Automatic duplicate detection (>85% similarity)

### Intelligent Priority Recommendations
- Multi-factor analysis (demand, revenue, competition)
- AI-generated reasoning and scoring

### High Performance
- Dapper for optimized SQL queries
- Connection pooling and caching
- Vector indexing for fast similarity search

## 📊 Database Schema

See [migrations](src/ProductIntelligence.Infrastructure/Data/Migrations/) for full schema.

Key tables:
- `domains` - Hierarchical business domains (ltree)
- `features` - Accepted features in roadmap
- `feature_requests` - Raw customer requests with embeddings
- `feature_votes` - Weighted voting system
- `feedback` - Customer feedback with sentiment analysis

## 🛠️ Technology Stack

- **.NET 9** - Modern C# with latest features
- **Dapper** - Micro-ORM for performance
- **FluentMigrator** - Database migrations
- **MediatR** - CQRS implementation
- **FluentValidation** - Request validation
- **Azure OpenAI** - GPT-4 & embeddings
- **PostgreSQL + pgvector** - Vector database

## 📝 Development

### Adding a New Migration

```bash
dotnet run --project src/ProductIntelligence.Infrastructure -- migrate up
```

### Code Style

- Follow C# conventions
- Use nullable reference types
- Async/await for all I/O operations
- Guard clauses for validation

## 🔒 Security

- Azure AD B2C authentication (coming soon)
- Role-based access control
- SQL injection prevention via parameterized queries
- Secure credential management

## 📈 Performance Targets

- API response time: < 100ms (p95)
- Vector search: < 50ms
- Concurrent users: 500+
- Database connections: Pooled, max 100

## 🤝 Contributing

1. Create feature branch
2. Write tests
3. Ensure all tests pass
4. Submit PR

## 📄 License

See [LICENSE](../../LICENSE)
