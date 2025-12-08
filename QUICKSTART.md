# 🚀 Quick Start Guide

## Backend Setup (.NET 9)

### 1. Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- [pgvector extension](https://github.com/pgvector/pgvector)

### 2. Database Setup

```bash
# Install PostgreSQL (macOS with Homebrew)
brew install postgresql@16
brew services start postgresql@16

# Create database
createdb product_intelligence

# Install extensions
psql -d product_intelligence << EOF
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "ltree";
CREATE EXTENSION IF NOT EXISTS "vector";
EOF
```

### 3. Configuration

Edit `src/backend/src/ProductIntelligence.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=product_intelligence;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-3-large"
  }
}
```

### 4. Run the API

```bash
cd src/backend/src/ProductIntelligence.API
dotnet run
```

API will be available at: `https://localhost:5001`
Swagger UI: `https://localhost:5001/swagger`

### 5. Run Tests

```bash
cd src/backend
dotnet test
```

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     CLEAN ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  API Layer (ASP.NET Core)                                   │
│  └─ Controllers, Middleware, SignalR Hubs                   │
│                                                              │
│  Application Layer (MediatR CQRS)                           │
│  └─ Commands, Queries, DTOs, Validators                     │
│                                                              │
│  Domain Layer (Core)                                        │
│  └─ Entities, Value Objects, Enums, Interfaces              │
│                                                              │
│  Infrastructure Layer (Dapper + Azure)                      │
│  └─ Repositories, Azure Services, Migrations                │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Key Features Implemented ✅

### 1. Hierarchical Domain Management
- Unlimited nesting using PostgreSQL `ltree`
- Efficient path-based queries
- Feature count aggregation

### 2. Feature Request System
- Vector embeddings for similarity search
- Duplicate detection (>85% similarity)
- Status tracking workflow

### 3. Dapper Data Access
- High-performance SQL queries
- Connection pooling
- PostgreSQL-specific optimizations

### 4. Database Migrations
- FluentMigrator for schema versioning
- Automatic migration on startup
- Support for ltree and vector types

### 5. Azure OpenAI Integration
- GPT-4o for chat completions
- Streaming responses
- Embeddings generation (1536 dimensions)

## Database Schema

```sql
-- Hierarchical domains
domains (ltree path, parent_domain_id)

-- Feature management
features (domain_id, status, priority, ai_priority_score)

-- Feature requests with embeddings
feature_requests (embedding_vector[1536], linked_feature_id)

-- Weighted voting
feature_votes (voter_tier, vote_weight)

-- Feedback with sentiment
feedback (content, sentiment, embedding_vector)

-- Strategic goals
domain_goals (domain_id, target_quarter, priority)
```

## Testing

Run all tests:
```bash
dotnet test
```

Run specific tests:
```bash
dotnet test --filter "FullyQualifiedName~DomainTests"
```

With coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverageDirectory=./coverage
```

## Development Workflow

1. **Create feature branch**
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Add domain entity** (if needed)
   - Update `ProductIntelligence.Core/Entities/`
   - Add repository interface in `Core/Interfaces/`

3. **Implement repository** (Dapper)
   - Create in `Infrastructure/Repositories/`
   - Write SQL queries directly

4. **Add use case** (CQRS)
   - Command: `Application/Commands/`
   - Query: `Application/Queries/`
   - Handler: Same folder

5. **Create controller endpoint**
   - Add to `API/Controllers/`
   - Use MediatR to dispatch

6. **Write tests**
   - Unit: `tests/ProductIntelligence.UnitTests/`

7. **Run and verify**
   ```bash
   dotnet build
   dotnet test
   dotnet run --project src/ProductIntelligence.API
   ```

## Next Steps

### Immediate (Week 1-2)
- [ ] Complete FeatureRepository implementation
- [ ] Add AI deduplication agent
- [ ] Implement priority recommendation agent
- [ ] Add Azure AI Search integration
- [ ] SignalR hub for streaming

### Short-term (Week 3-4)
- [ ] Authentication (Azure AD B2C)
- [ ] Authorization middleware
- [ ] Document upload service
- [ ] Background worker for embeddings
- [ ] Redis caching layer

### Medium-term (Week 5-8)
- [ ] Flutter frontend
- [ ] Real-time chat interface
- [ ] Analytics dashboard
- [ ] Jira/ADO integration
- [ ] Production deployment (Azure)

## Troubleshooting

### PostgreSQL Connection Issues
```bash
# Check if PostgreSQL is running
brew services list

# Restart PostgreSQL
brew services restart postgresql@16

# Test connection
psql -d product_intelligence -c "SELECT version();"
```

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Migration Issues
```bash
# Migrations run automatically on startup
# To run manually:
cd src/ProductIntelligence.API
dotnet run
```

## Performance Tips

1. **Connection Pooling** - Already configured in Npgsql
2. **Indexes** - Created in migrations (GIST, IVFFLAT)
3. **Async/Await** - Use everywhere for I/O
4. **Projections** - Use `SELECT` specific columns, not `*`
5. **Vector Search** - Adjust `lists` parameter in IVFFLAT index

## Security Checklist

- [x] Parameterized queries (SQL injection prevention)
- [x] Nullable reference types enabled
- [ ] Azure AD authentication
- [ ] RBAC authorization
- [ ] Rate limiting
- [ ] Input validation (FluentValidation)
- [ ] CORS configuration
- [ ] HTTPS enforcement

## Resources

- [.NET 9 Docs](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [Dapper Docs](https://github.com/DapperLib/Dapper)
- [pgvector Guide](https://github.com/pgvector/pgvector)
- [Azure OpenAI](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [MediatR](https://github.com/jbogard/MediatR)
