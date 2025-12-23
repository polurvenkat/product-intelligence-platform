namespace ProductIntelligence.Tests.IntegrationTests;

/// <summary>
/// Base class for integration tests that call actual HTTP endpoints.
/// Set API_BASE_URL environment variable to configure the target API.
/// Default: http://localhost:5000
/// </summary>
public class IntegrationTestBase : IAsyncLifetime
{
    protected HttpClient Client { get; private set; } = null!;
    protected string BaseUrl { get; private set; } = null!;

    public IntegrationTestBase()
    {
        // Get base URL from environment variable or use default
        BaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";
    }

    public Task InitializeAsync()
    {
        Client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to wait for API to be ready (useful in CI/CD)
    /// </summary>
    protected async Task<bool> WaitForApiAsync(TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < maxWait)
        {
            try
            {
                var response = await Client.GetAsync("/health");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // API not ready yet
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return false;
    }
}
