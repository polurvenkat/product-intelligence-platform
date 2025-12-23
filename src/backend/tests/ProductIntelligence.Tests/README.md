# Product Intelligence Platform - Integration Tests

This project contains comprehensive integration tests for the Product Intelligence Platform API. The tests call actual HTTP endpoints and can be run against any deployment of the API.

## Test Structure

```
ProductIntelligence.Tests/
├── IntegrationTests/
│   ├── IntegrationTestBase.cs          # Base class with HTTP client setup
│   ├── DomainsControllerTests.cs       # Tests for /api/domains endpoints
│   ├── FeaturesControllerTests.cs      # Tests for /api/features endpoints
│   ├── FeatureRequestsControllerTests.cs # Tests for /api/feature-requests endpoints
│   ├── FeedbackControllerTests.cs      # Tests for /api/feedback endpoints
│   └── VotesControllerTests.cs         # Tests for /api/votes endpoints
└── UnitTests/                          # (Future: Unit tests go here)
```

## Running Tests Locally

### Prerequisites

1. **Start the API locally:**
   ```bash
   cd src/backend
   dotnet run --project src/ProductIntelligence.API
   ```
   
   The API should be running at `http://localhost:5000`

2. **Ensure the database is set up:**
   - PostgreSQL with pgvector extension must be running
   - Database schema must be initialized
   - Connection string configured in `appsettings.Development.json`

### Run All Tests

```bash
cd src/backend/tests/ProductIntelligence.Tests
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~DomainsControllerTests"
dotnet test --filter "FullyQualifiedName~FeaturesControllerTests"
dotnet test --filter "FullyQualifiedName~FeatureRequestsControllerTests"
```

### Run Specific Test Method

```bash
dotnet test --filter "FullyQualifiedName~DomainsControllerTests.CreateDomain_WithValidData_ReturnsCreatedDomain"
```

### Run Tests with Detailed Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Running Tests in CI/CD

### Environment Variable Configuration

The tests use the `API_BASE_URL` environment variable to determine which API to test against:

```bash
# Local (default)
export API_BASE_URL="http://localhost:5000"
dotnet test

# Staging
export API_BASE_URL="https://api-staging.yourcompany.com"
dotnet test

# Production (be careful!)
export API_BASE_URL="https://api.yourcompany.com"
dotnet test
```

### GitHub Actions Example

```yaml
name: Integration Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: pgvector/pgvector:pg17
        env:
          POSTGRES_DB: product_intelligence
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Start API
        run: |
          cd src/backend
          dotnet run --project src/ProductIntelligence.API &
          sleep 10  # Wait for API to start
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=product_intelligence;Username=postgres;Password=postgres"
      
      - name: Run Integration Tests
        run: |
          cd src/backend/tests/ProductIntelligence.Tests
          dotnet test --logger "trx;LogFileName=test-results.trx"
        env:
          API_BASE_URL: "http://localhost:5000"
      
      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: '**/test-results.trx'
```

### Azure DevOps Example

```yaml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  API_BASE_URL: 'http://localhost:5000'

steps:
- task: UseDotNet@2
  inputs:
    version: '10.0.x'

- script: |
    docker run -d --name postgres \
      -e POSTGRES_DB=product_intelligence \
      -e POSTGRES_USER=postgres \
      -e POSTGRES_PASSWORD=postgres \
      -p 5432:5432 \
      pgvector/pgvector:pg17
  displayName: 'Start PostgreSQL'

- script: |
    cd src/backend
    dotnet run --project src/ProductIntelligence.API &
    sleep 10
  displayName: 'Start API'
  env:
    ConnectionStrings__DefaultConnection: 'Host=localhost;Database=product_intelligence;Username=postgres;Password=postgres'

- script: |
    cd src/backend/tests/ProductIntelligence.Tests
    dotnet test --logger trx
  displayName: 'Run Integration Tests'
  env:
    API_BASE_URL: $(API_BASE_URL)

- task: PublishTestResults@2
  condition: always()
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
```

## Test Coverage

### DomainsControllerTests (10 tests)
- ✅ Create domain with valid data
- ✅ Create domain with empty name (validation)
- ✅ Create domain with parent (hierarchy)
- ✅ Get domain by ID
- ✅ Get non-existent domain (404)
- ✅ Update domain
- ✅ Update with mismatched ID (400)
- ✅ Delete domain
- ✅ Delete non-existent domain (404)
- ✅ Get domain hierarchy by organization

### FeaturesControllerTests (11 tests)
- ✅ Create feature with valid data
- ✅ Create feature with invalid domain (400)
- ✅ Get feature by ID
- ✅ Get non-existent feature (404)
- ✅ Update feature
- ✅ Delete feature
- ✅ Get features by domain
- ✅ Get features by status
- ✅ Update feature status
- ✅ Update feature priority
- ✅ Update with AI priority score and reasoning

### FeatureRequestsControllerTests (13 tests)
- ✅ Submit feature request with valid data
- ✅ Submit with empty title (validation)
- ✅ Submit with all fields (stores all data)
- ✅ Get pending requests
- ✅ Get pending requests with limit
- ✅ Get requests by status
- ✅ Get requests by feature
- ✅ Update request status
- ✅ Update status with invalid ID (404)
- ✅ Mark request as duplicate
- ✅ Get duplicate requests
- ✅ Submit from different customer tiers
- ✅ Verify tier-specific data storage

### FeedbackControllerTests (11 tests)
- ✅ Submit feedback for feature
- ✅ Submit feedback for feature request
- ✅ Submit with empty content (validation)
- ✅ Submit without target (400)
- ✅ Get feedback by ID
- ✅ Get non-existent feedback (404)
- ✅ Get feedback by feature
- ✅ Get feedback by request
- ✅ Submit with different sentiments
- ✅ Submit with customer information
- ✅ Track feedback from different sources

### VotesControllerTests (11 tests)
- ✅ Vote for feature
- ✅ Vote with empty email (validation)
- ✅ Duplicate vote (409)
- ✅ Votes from different customer tiers
- ✅ Get votes by feature
- ✅ Get vote count
- ✅ Remove vote
- ✅ Remove without email (400)
- ✅ Multiple voters from same company
- ✅ Vote with user ID association
- ✅ Complete vote workflow

**Total: 56 comprehensive integration tests covering all CRUD operations and edge cases**

## Test Data Management

### Data Isolation

⚠️ **Warning:** These tests use the actual database configured in the API's connection string. They will create, modify, and potentially delete real data.

**Best Practices:**
1. Use a separate test database (e.g., `product_intelligence_test`)
2. Run tests against local or dedicated test environments only
3. Never run integration tests against production

### Cleaning Up Test Data

The tests create data during execution. To clean up after tests:

```sql
-- Truncate all test data (use with caution!)
TRUNCATE TABLE feedback, feature_votes, feature_requests, features, domain_goals, domains CASCADE;
```

### Database Reset Script

Create a reset script for your test database:

```bash
#!/bin/bash
# reset-test-db.sh

psql -h localhost -U postgres -d product_intelligence_test -c "
TRUNCATE TABLE feedback, feature_votes, feature_requests, features, domain_goals, domains CASCADE;
"
```

## Debugging Tests

### View API Logs

When tests fail, check the API logs:

```bash
# If API is running in terminal
# Logs appear in the console

# If API is running in background
tail -f /tmp/api.log
```

### Run Single Test with Breakpoints

1. Start API in debug mode in VS Code
2. Set breakpoints in test code
3. Run single test:
   ```bash
   dotnet test --filter "FullyQualifiedName~CreateDomain_WithValidData_ReturnsCreatedDomain"
   ```

### Check API Health Before Tests

```bash
curl http://localhost:5000/health
```

Expected response: `200 OK`

## Writing New Tests

### Template for New Test Class

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductIntelligence.Application.DTOs;

namespace ProductIntelligence.Tests.IntegrationTests;

public class YourControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task YourTest_WithValidData_ReturnsExpectedResult()
    {
        // Arrange
        var command = new YourCommand
        {
            Property1 = "Value1",
            Property2 = "Value2"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/your-endpoint", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<YourDto>();
        result.Should().NotBeNull();
        result!.Property1.Should().Be("Value1");
    }
}
```

### FluentAssertions Patterns

```csharp
// Status codes
response.StatusCode.Should().Be(HttpStatusCode.OK);
response.StatusCode.Should().Be(HttpStatusCode.Created);
response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
response.StatusCode.Should().Be(HttpStatusCode.NotFound);

// Objects
result.Should().NotBeNull();
result!.Id.Should().NotBeEmpty();
result.Name.Should().Be("Expected Name");

// Collections
list.Should().HaveCount(5);
list.Should().HaveCountGreaterThan(0);
list.Should().Contain(item => item.Name == "Test");
list.Should().OnlyContain(item => item.Status == "Active");

// Nulls and empty
value.Should().BeNull();
value.Should().NotBeNull();
string.IsNullOrEmpty(value).Should().BeFalse();
```

## Troubleshooting

### Tests Fail with Connection Errors

**Problem:** Cannot connect to API
**Solution:**
1. Ensure API is running: `curl http://localhost:5000/health`
2. Check firewall settings
3. Verify API_BASE_URL is correct

### Tests Fail with 500 Internal Server Error

**Problem:** API errors during test execution
**Solution:**
1. Check API logs for stack traces
2. Verify database connection string
3. Ensure database schema is up to date
4. Check pgvector extension is installed

### Tests Pass Individually but Fail Together

**Problem:** Test interdependencies or shared state
**Solution:**
1. Review test data - ensure each test creates its own data
2. Use unique identifiers (GUIDs) to avoid conflicts
3. Consider cleaning up data after each test

### Slow Test Execution

**Problem:** Tests take too long
**Solution:**
1. Run tests in parallel: `dotnet test --parallel`
2. Use test filters to run specific subsets
3. Optimize database queries in API
4. Consider using test database with smaller dataset

## Contributing

When adding new integration tests:

1. **Follow naming conventions:** `MethodName_Scenario_ExpectedBehavior`
2. **Use FluentAssertions** for readable assertions
3. **Create isolated test data** - don't rely on existing data
4. **Test both success and failure cases**
5. **Add meaningful descriptions** in test method names
6. **Update this README** with new test coverage

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [ASP.NET Core Testing](https://learn.microsoft.com/en-us/aspnet/core/test/)
