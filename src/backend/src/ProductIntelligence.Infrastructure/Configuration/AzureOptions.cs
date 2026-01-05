namespace ProductIntelligence.Infrastructure.Configuration;

public class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ManagedIdentityClientId { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4o";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-large";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
}

public class AzureAISearchOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "product-intelligence";
}

public class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
}

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultExpirationMinutes { get; set; } = 60;
}

public class AzureDevOpsOptions
{
    public string OrganizationUrl { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
}
