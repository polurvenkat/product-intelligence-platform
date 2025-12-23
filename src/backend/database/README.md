# Database Setup

## Prerequisites

1. **PostgreSQL 16+** installed
2. **pgvector extension** available (install via: `sudo apt install postgresql-16-pgvector` or brew on macOS)
3. Database user with CREATE privileges

## Quick Setup

### 1. Create Database

```bash
createdb product_intelligence
```

Or using psql:
```sql
CREATE DATABASE product_intelligence;
```

### 2. Run Migrations

Execute migrations in order:

```bash
# Connect to database
psql -d product_intelligence

# Run migrations
\i migrations/001_initial_schema.sql
\i migrations/002_vector_similarity_functions.sql
```

Or run all at once:
```bash
psql -d product_intelligence -f migrations/001_initial_schema.sql
psql -d product_intelligence -f migrations/002_vector_similarity_functions.sql
```

### 3. Verify Installation

```sql
-- Check pgvector extension
SELECT * FROM pg_extension WHERE extname = 'vector';

-- Check tables
\dt

-- Check functions
\df fn_*

-- Verify vector indexes
SELECT 
    tablename, 
    indexname, 
    indexdef 
FROM pg_indexes 
WHERE indexname LIKE '%embedding%';
```

## Connection String

Update your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=product_intelligence;Username=your_user;Password=your_password"
  }
}
```

For local development, you can also use:
```bash
# Set user secret (recommended)
cd src/backend/src/ProductIntelligence.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=product_intelligence;Username=postgres;Password=your_password"

cd ../../ProductIntelligence.Workers
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=product_intelligence;Username=postgres;Password=your_password"
```

## Database Schema

### Tables

- **domains**: Hierarchical product areas with materialized path
- **domain_goals**: Strategic goals per domain
- **features**: Product features with AI priority scoring
- **feature_requests**: Customer requests with embedding vectors
- **feedback**: User feedback with AI sentiment analysis
- **feature_votes**: Tier-weighted voting system

### Vector Fields (1536 dimensions - text-embedding-3-large)

- `features.embedding_vector`
- `feature_requests.embedding_vector`
- `feedback.embedding_vector`

### Key Functions

- `fn_feature_find_similar()`: Semantic search for features
- `fn_feature_request_find_similar()`: Duplicate detection for requests
- `fn_feedback_find_similar()`: Group related feedback
- `fn_feature_vote_stats()`: Calculate weighted vote scores
- `fn_domain_get_path()`: Get full domain hierarchy path

## Performance Tuning

### HNSW Index Parameters

The migrations create HNSW indexes for vector similarity. Default parameters work well for most cases, but you can tune for your data:

```sql
-- Rebuild with custom parameters (more accurate, slower build)
DROP INDEX idx_features_embedding_vector;
CREATE INDEX idx_features_embedding_vector 
ON features 
USING hnsw (embedding_vector vector_cosine_ops)
WITH (m = 32, ef_construction = 200);
```

### Analyze Tables

After loading data:
```sql
ANALYZE domains;
ANALYZE features;
ANALYZE feature_requests;
ANALYZE feedback;
ANALYZE feature_votes;
```

## Sample Data (Optional)

```sql
-- Insert root domain
INSERT INTO domains (name, description, path) 
VALUES ('Product', 'Root product domain', '/Product');

-- Insert child domain
INSERT INTO domains (name, description, path, parent_id) 
SELECT 'User Experience', 'UX improvements', '/Product/User Experience', id 
FROM domains WHERE path = '/Product';

-- Insert a feature
INSERT INTO features (domain_id, title, description, status, priority) 
SELECT id, 'Dark Mode Support', 'Add dark mode theme option for better user experience', 'Proposed', 'High' 
FROM domains WHERE path = '/Product/User Experience';
```

## Backup & Restore

### Backup
```bash
pg_dump product_intelligence > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restore
```bash
psql product_intelligence < backup_20241223_120000.sql
```

## Troubleshooting

### pgvector not found
```bash
# Ubuntu/Debian
sudo apt install postgresql-16-pgvector

# macOS (Homebrew)
brew install pgvector

# Then restart PostgreSQL
sudo systemctl restart postgresql  # Linux
brew services restart postgresql   # macOS
```

### Permission denied
```sql
-- Grant necessary permissions
GRANT ALL PRIVILEGES ON DATABASE product_intelligence TO your_user;
GRANT ALL ON ALL TABLES IN SCHEMA public TO your_user;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO your_user;
```

### Slow vector queries
```sql
-- Check if HNSW indexes exist
\d+ features

-- Verify index is being used
EXPLAIN ANALYZE 
SELECT * FROM fn_feature_find_similar(
    (SELECT embedding_vector FROM features LIMIT 1),
    0.7,
    10
);
```

## Next Steps

1. Start the API: `cd src/backend/src/ProductIntelligence.API && dotnet run`
2. Start the Workers: `cd src/backend/src/ProductIntelligence.Workers && dotnet run`
3. Test endpoints with Swagger: `http://localhost:5000/swagger`
