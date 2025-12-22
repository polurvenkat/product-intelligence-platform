# Azure OpenAI Setup - Complete ✅

## Resource Configuration

| Property | Value |
|----------|-------|
| **Subscription** | FarmQA DevTest Subscription |
| **Resource Group** | rg-projectmangement-model |
| **Resource Name** | ai-projectmanagement-model |
| **Location** | centralus |
| **SKU** | S0 (Standard) |
| **Endpoint** | https://ai-projectmanagement-model.openai.azure.com/ |
| **Network Security** | ✅ Private Endpoint Only (Public Access Disabled) |

## Deployed Models

### 1. GPT-4o (Chat/Reasoning)
- **Deployment Name**: `gpt-4o`
- **Model Version**: 2024-08-06
- **SKU**: Standard
- **Capacity**: 10K tokens/min
- **Capabilities**: Chat completion, Assistants, Agents, JSON schema response
- **Max Context**: 128,000 tokens
- **Max Output**: 16,384 tokens

### 2. text-embedding-3-large (Embeddings)
- **Deployment Name**: `text-embedding-3-large`
- **Model Version**: 1
- **SKU**: GlobalStandard
- **Capacity**: 10K tokens/min
- **Dimensions**: 3072 (default) or 1536 (configurable)
- **Max Inputs per Request**: 2048

## Application Configuration

### User Secrets (Development) ✅ Configured

Both projects (API and Workers) are configured with:

```bash
AzureOpenAI:Endpoint = https://ai-projectmanagement-model.openai.azure.com/
AzureOpenAI:ApiKey = [STORED SECURELY]
AzureOpenAI:DeploymentName = gpt-4o
AzureOpenAI:EmbeddingDeploymentName = text-embedding-3-large
```

### Network Security Configuration ✅

**Private Endpoint**:
- Name: `pe-openai-projectmanagement`
- Private IP: `172.16.0.4`
- VNet: `ai-vnet`
- Subnet: `ai-subnet-1`
- Status: Approved and Connected

**Private DNS Zone**:
- Zone: `privatelink.openai.azure.com`
- A Record: `ai-projectmanagement-model.privatelink.openai.azure.com` → `172.16.0.4`
- VNet Link: `openai-dns-link` (Connected to ai-vnet)

**Public Access**: ✅ **DISABLED** (Enterprise-Grade Security)
- All traffic must flow through private endpoint
- No internet-facing access allowed
- Network ACL: Default action is Deny

## Rate Limits

### GPT-4o
- 10 requests per 10 seconds
- 10,000 tokens per minute

### text-embedding-3-large
- 10 requests per 10 seconds
- 10,000 tokens per minute

## Quick Commands

### View Resource
```bash
az cognitiveservices account show \
  --name ai-projectmanagement-model \
  --resource-group rg-projectmangement-model
```

### List Deployments
```bash
az cognitiveservices account deployment list \
  --name ai-projectmanagement-model \
  --resource-group rg-projectmangement-model -o table
```

### Get Keys
```bash
az cognitiveservices account keys list \
  --name ai-projectmanagement-model \
  --resource-group rg-projectmangement-model
```

### Rotate Keys
```bash
az cognitiveservices account keys regenerate \
  --name ai-projectmanagement-model \
  --resource-group rg-projectmangement-model \
  --key-name key1
```

## Next Steps (Optional Security Enhancements)

### 1. ✅ Private Endpoint - CONFIGURED
Private endpoint is now active and public access is disabled. Your Azure OpenAI is fully secured!

**Current Configuration**:
- Private IP: 172.16.0.4
- Custom subdomain: ai-projectmanagement-model.openai.azure.com
- DNS resolution through privatelink.openai.azure.com
- Network isolation enforced

**Important**: Your application must run within the VNet (ai-vnet) or have connectivity to it via:
- VPN Gateway
- ExpressRoute
- VNet Peering
- Bastion Host

### 2. Managed Identity (Recommended Next Step)
```bash
# Enable for App Service/Container
az webapp identity assign \
  --name your-app-name \
  --resource-group rg-projectmangement-model

# Grant Cognitive Services OpenAI User role
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee <principal-id> \
  --scope /subscriptions/68d6f67f-0b97-4796-a5ca-2d967eff244a/resourceGroups/rg-projectmangement-model/providers/Microsoft.CognitiveServices/accounts/ai-projectmanagement-model
```

### 3. Diagnostic Logging
```bash
# Enable diagnostic logs to Log Analytics
az monitor diagnostic-settings create \
  --name openai-diagnostics \
  --resource /subscriptions/68d6f67f-0b97-4796-a5ca-2d967eff244a/resourceGroups/rg-projectmangement-model/providers/Microsoft.CognitiveServices/accounts/ai-projectmanagement-model \
  --workspace <log-analytics-workspace-id> \
  --logs '[{"category":"Audit","enabled":true},{"category":"RequestResponse","enabled":true}]' \
  --metrics '[{"category":"AllMetrics","enabled":true}]'
```

## Testing the Connection

### From API
```bash
cd src/backend/src/ProductIntelligence.API
dotnet run
```

### Test Endpoint
```bash
# Create a feature request (will auto-generate embeddings and sentiment)
curl -X POST http://localhost:5000/api/feature-requests \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Add dark mode",
    "description": "Users want dark mode support for better night-time usage",
    "requesterName": "John Doe",
    "requesterCompany": "Acme Corp",
    "requesterTier": "Enterprise"
  }'
```

## Monitoring

### View Metrics in Azure Portal
- Navigate to: Azure Portal → Resource Group → ai-projectmanagement-model
- Click: Metrics
- Available metrics:
  - Total Calls
  - Total Tokens
  - Model Response Time
  - Processed Prompt Tokens
  - Generated Completion Tokens

### View Logs
- Navigate to: Logs section
- Query examples:
  ```kusto
  AzureDiagnostics
  | where ResourceProvider == "MICROSOFT.COGNITIVESERVICES"
  | where Category == "RequestResponse"
  | order by TimeGenerated desc
  ```

## Cost Optimization

### Current Pricing (as of Dec 2024)
- **GPT-4o**: $2.50 per 1M input tokens, $10.00 per 1M output tokens
- **text-embedding-3-large**: $0.13 per 1M tokens

### Tips
1. Use caching for repeated requests
2. Limit max tokens in responses
3. Batch embedding requests
4. Monitor usage via Azure Cost Management
5. Set up budget alerts

## Troubleshooting

### Common Issues

1. **401 Unauthorized**: Check API key in user secrets
2. **404 Not Found**: Verify deployment names match configuration
3. **429 Rate Limited**: Increase capacity or add retry logic
4. **Connection Timeout**: Check network/firewall settings

### Support
- Azure Support: https://portal.azure.com → Help + support
- OpenAI Documentation: https://learn.microsoft.com/azure/ai-services/openai/
