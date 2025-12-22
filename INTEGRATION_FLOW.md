# Product Intelligence Platform - Integration Flow

## Complete System Architecture & Integration Flow

### Full Architecture Diagram (For GitHub/VS Code)

```mermaid
graph TB
    subgraph "Client Layer"
        Client[Flutter/Web Client]
        API_Call[HTTP Request]
    end

    subgraph "API Layer - ASP.NET Core"
        Controller[Controllers<br/>DomainsController<br/>FeaturesController<br/>FeatureRequestsController<br/>FeedbackController<br/>VotesController<br/>SearchController]
        
        subgraph "CORS & Middleware"
            CORS[CORS Policy<br/>localhost:3000, 5173]
            Auth[Authorization<br/>Future: JWT]
            ErrorHandler[Error Handler]
        end
    end

    subgraph "Application Layer - CQRS with MediatR"
        subgraph "Commands - Write Operations"
            CMD1[CreateDomainCommand]
            CMD2[CreateFeatureCommand]
            CMD3[SubmitFeatureRequestCommand]
            CMD4[SubmitFeedbackCommand]
            CMD5[VoteForFeatureCommand]
            CMD6[LinkRequestToFeatureCommand]
            CMD7[MarkRequestAsDuplicateCommand]
            CMD8[UpdateRequestStatusCommand]
        end
        
        subgraph "Command Handlers"
            Handler1[CreateDomainCommandHandler]
            Handler2[CreateFeatureCommandHandler]
            Handler3[SubmitFeatureRequestCommandHandler]
            Handler4[SubmitFeedbackCommandHandler]
            Handler5[VoteForFeatureCommandHandler]
            Handler6[LinkRequestCommandHandler]
            Handler7[MarkDuplicateCommandHandler]
            Handler8[UpdateStatusCommandHandler]
        end
        
        subgraph "Queries - Read Operations"
            QRY1[GetDomainsQuery]
            QRY2[GetFeaturesQuery]
            QRY3[GetFeatureRequestsQuery]
            QRY4[SearchFeaturesQuery]
            QRY5[SearchFeatureRequestsQuery]
            QRY6[FilterFeaturesQuery]
            QRY7[GetFeedbackQuery]
            QRY8[GetVotesQuery]
        end
        
        subgraph "Query Handlers"
            QHandler1[GetDomainsQueryHandler]
            QHandler2[GetFeaturesQueryHandler]
            QHandler3[GetRequestsQueryHandler]
            QHandler4[SearchFeaturesQueryHandler]
            QHandler5[SearchRequestsQueryHandler]
            QHandler6[FilterFeaturesQueryHandler]
            QHandler7[GetFeedbackQueryHandler]
            QHandler8[GetVotesQueryHandler]
        end
        
        subgraph "Behaviors - Cross-Cutting"
            Validation[ValidationBehavior<br/>FluentValidation]
            Logging[Logging Behavior]
        end
    end

    subgraph "Core Layer - Domain Logic"
        subgraph "Entities"
            E1[Domain Entity<br/>- Validation<br/>- Business Rules]
            E2[Feature Entity<br/>- Status Transitions<br/>- Priority Logic]
            E3[FeatureRequest Entity<br/>- Deduplication<br/>- Linking Logic]
            E4[Feedback Entity<br/>- Sentiment Analysis]
            E5[FeatureVote Entity<br/>- Weight Calculation]
        end
        
        subgraph "Interfaces"
            Repo[IRepository Interfaces<br/>IDomainRepository<br/>IFeatureRepository<br/>IFeatureRequestRepository<br/>IFeedbackRepository<br/>IFeatureVoteRepository]
        end
        
        subgraph "Enums"
            EnumTypes[FeatureStatus<br/>RequestStatus<br/>Priority<br/>CustomerTier<br/>Sentiment]
        end
    end

    subgraph "Infrastructure Layer"
        subgraph "Repositories - Data Access"
            R1[DomainRepository<br/>Dapper SQL]
            R2[FeatureRepository<br/>Dapper + pgvector]
            R3[FeatureRequestRepository<br/>Dapper + pgvector]
            R4[FeedbackRepository<br/>Dapper SQL]
            R5[FeatureVoteRepository<br/>Dapper SQL]
        end
        
        subgraph "AI Services"
            AI[AzureOpenAIService]
            
            subgraph "AI Operations"
                AI_Chat[CompleteChatAsync<br/>GPT-4o<br/>- Sentiment Analysis<br/>- Priority Calculation<br/>- Feature Reasoning]
                AI_Embed[GenerateEmbeddingAsync<br/>text-embedding-3-large<br/>- 1536 dimensions<br/>- Semantic Search]
                AI_Stream[StreamChatAsync<br/>Real-time responses]
            end
        end
        
        subgraph "Database Factory"
            DBFactory[IDbConnectionFactory<br/>NpgsqlConnectionFactory]
        end
        
        subgraph "Agents"
            DedupeAgent[FeatureDeduplicationAgent<br/>- Vector Similarity<br/>- Threshold 0.85<br/>- Duplicate Detection]
        end
    end

    subgraph "Database - PostgreSQL + pgvector"
        subgraph "Tables"
            T1[(domains table)]
            T2[(features table)]
            T3[(feature_requests table<br/>+ embedding_vector)]
            T4[(feedback table<br/>+ embedding_vector)]
            T5[(feature_votes table)]
            T6[(domain_goals table)]
        end
        
        subgraph "Stored Procedures"
            SP1[fn_feature_find_similar<br/>Vector Similarity Search]
            SP2[fn_feature_request_find_similar<br/>Cosine Distance Search]
        end
        
        subgraph "Vector Operations"
            VectorOps[pgvector Extension<br/>- Cosine Distance<br/>- <=> operator<br/>- HNSW Indexing]
        end
    end

    subgraph "Background Workers - .NET Hosted Services"
        subgraph "Worker Processes"
            W1[FeatureRequestProcessorWorker<br/>Interval: 30s<br/>Batch: 10]
            W2[PriorityCalculationWorker<br/>Interval: 6h<br/>Batch: 20]
            W3[EmbeddingGeneratorWorker<br/>Interval: 5m<br/>Batch: 15]
            W4[DocumentProcessorWorker<br/>Future: Document AI]
        end
        
        subgraph "Worker Logic"
            WL1[Poll for requests<br/>without embeddings]
            WL2[Poll active features<br/>for priority update]
            WL3[Poll features<br/>without embeddings]
        end
    end

    subgraph "Azure Services"
        subgraph "Azure OpenAI"
            AZ_OAI[ai-projectmanagement-model<br/>centralus<br/>S0 SKU]
            
            subgraph "Deployed Models"
                Model1[gpt-4o<br/>2024-08-06<br/>10K TPM<br/>128K context]
                Model2[text-embedding-3-large<br/>v1<br/>10K TPM<br/>GlobalStandard]
            end
        end
        
        subgraph "Network Security"
            PE[Private Endpoint<br/>pe-openai-projectmanagement<br/>172.16.0.4]
            DNS[Private DNS Zone<br/>privatelink.openai.azure.com]
            VNET[VNet: ai-vnet<br/>Subnet: ai-subnet-1]
            PubAccess[Public Access<br/>Enabled: Dev Only<br/>Disabled: Production]
        end
    end

    %% Client to API Flow
    Client -->|1. HTTP POST/GET| API_Call
    API_Call -->|2. Request| CORS
    CORS -->|3. Validate Origin| Auth
    Auth -->|4. Route| Controller
    
    %% Controller to CQRS
    Controller -->|5a. Write: Send Command| CMD3
    Controller -->|5b. Read: Send Query| QRY4
    
    %% Command Flow with Validation
    CMD3 -->|6. Validate| Validation
    Validation -->|7. Pass| Handler3
    Handler3 -->|8. Load/Create Entity| E3
    
    %% Entity Business Logic
    E3 -->|9. Validate Business Rules| E3
    E3 -->|10. Check Duplicates| DedupeAgent
    
    %% Deduplication Flow
    DedupeAgent -->|11a. Generate Embedding| AI_Embed
    AI_Embed -->|11b. Return float array| DedupeAgent
    DedupeAgent -->|12. Find Similar| R3
    R3 -->|13. Query pgvector| SP2
    SP2 -->|14. Cosine Distance| VectorOps
    VectorOps -->|15. Return matches| R3
    R3 -->|16. Similar requests| DedupeAgent
    DedupeAgent -->|17. Decision: Duplicate?| Handler3
    
    %% Save Request Flow
    Handler3 -->|18. Save if unique| R3
    R3 -->|19. INSERT| T3
    T3 -->|20. Return ID| R3
    R3 -->|21. Entity| Handler3
    Handler3 -->|22. Map to DTO| Controller
    Controller -->|23. HTTP 201| Client
    
    %% Query Flow
    QRY4 -->|24. Validate| Validation
    Validation -->|25. Execute| QHandler4
    QHandler4 -->|26. Generate Embedding| AI_Embed
    AI_Embed -->|27. Return float array| QHandler4
    QHandler4 -->|28. Semantic Search| R2
    R2 -->|29. Call SP| SP1
    SP1 -->|30. Vector Search| VectorOps
    VectorOps -->|31. Top N results| R2
    R2 -->|32. Features| QHandler4
    QHandler4 -->|33. Load Domains<br/>Parallel| R1
    R1 -->|34. Domain data| QHandler4
    QHandler4 -->|35. Map to SearchResultDto| Controller
    Controller -->|36. HTTP 200 JSON| Client
    
    %% Feedback with AI Sentiment
    CMD4 -->|37. Submit Feedback| Handler4
    Handler4 -->|38. Analyze Sentiment| AI_Chat
    AI_Chat -->|39. POST to Azure| Model1
    Model1 -->|40. System + User Messages| Model1
    Model1 -->|41. JSON Response<br/>sentiment + confidence| AI_Chat
    AI_Chat -->|42. Parse Result| Handler4
    Handler4 -->|43. Set Sentiment| E4
    Handler4 -->|44. Save| R4
    R4 -->|45. INSERT| T4
    
    %% Vote Weight Calculation
    CMD5 -->|46. Vote for Feature| Handler5
    Handler5 -->|47. Load Feature| R2
    R2 -->|48. Feature Entity| Handler5
    Handler5 -->|49. Calculate Weight| E5
    E5 -->|50. Enterprise: 3x<br/>Professional: 2x<br/>Starter: 1x| E5
    Handler5 -->|51. Save Vote| R5
    R5 -->|52. INSERT| T5
    
    %% Request Lifecycle
    CMD6 -->|53. Link to Feature| Handler6
    Handler6 -->|54. Validate Both IDs| Handler6
    Handler6 -->|55. Load Request| R3
    R3 -->|56. Request Entity| Handler6
    Handler6 -->|57. entity.LinkToFeature| E3
    E3 -->|58. Status: Accepted<br/>LinkedFeatureId set| E3
    Handler6 -->|59. Update| R3
    R3 -->|60. UPDATE| T3
    
    %% Background Worker Flow
    W1 -->|61. Every 30s| WL1
    WL1 -->|62. GetPendingAsync| R3
    R3 -->|63. WHERE embedding_vector IS NULL| T3
    T3 -->|64. Unprocessed requests| WL1
    WL1 -->|65. For each request| AI_Embed
    AI_Embed -->|66. Generate Embedding| Model2
    Model2 -->|67. 1536-dim vector| AI_Embed
    AI_Embed -->|68. Return float array| WL1
    WL1 -->|69. UpdateEmbeddingAsync| R3
    R3 -->|70. UPDATE embedding_vector| T3
    
    %% Priority Calculation Worker
    W2 -->|71. Every 6h| WL2
    WL2 -->|72. GetAllAsync| R2
    R2 -->|73. Active features<br/>Accepted/InProgress| T2
    T2 -->|74. Feature list| WL2
    WL2 -->|75. For each feature| AI_Chat
    AI_Chat -->|76. Analyze Priority<br/>Business Value<br/>Effort<br/>Impact<br/>Strategic Fit| Model1
    Model1 -->|77. JSON: score + reasoning| AI_Chat
    AI_Chat -->|78. Parse Result| WL2
    WL2 -->|79. Score changed > 0.1?| WL2
    WL2 -->|80. UpdatePriorityAsync| R2
    R2 -->|81. UPDATE ai_priority_score| T2
    
    %% Azure OpenAI Connection
    AI -->|82. Initialize Client| AZ_OAI
    AZ_OAI -->|83. Check Access| PubAccess
    PubAccess -->|84a. Dev: Public Internet| AZ_OAI
    PubAccess -->|84b. Prod: Private Endpoint| PE
    PE -->|85. Route through VNet| VNET
    VNET -->|86. DNS Lookup| DNS
    DNS -->|87. Resolve to 172.16.0.4| PE
    PE -->|88. Secure Connection| AZ_OAI
    AZ_OAI -->|89. Authenticate<br/>AzureKeyCredential| Model1
    
    %% Database Connections
    DBFactory -->|90. CreateConnectionAsync| T1
    DBFactory -->|91. NpgsqlConnection<br/>Connection String| T2
    DBFactory -->|92. connection.OpenAsync| T3
    
    %% Error Handling
    ErrorHandler -.->|Error: 400/404/500| Controller
    Validation -.->|ValidationException| ErrorHandler
    Handler3 -.->|InvalidOperationException<br/>Duplicate found| ErrorHandler
    R3 -.->|KeyNotFoundException<br/>ID not found| ErrorHandler
    AI -.->|RateLimitException<br/>429 from Azure| ErrorHandler

    style Client fill:#e1f5ff
    style Controller fill:#fff4e1
    style Handler3 fill:#e8f5e9
    style QHandler4 fill:#e8f5e9
    style E3 fill:#f3e5f5
    style R3 fill:#fce4ec
    style AI fill:#fff3e0
    style Model1 fill:#ffe0b2
    style Model2 fill:#ffe0b2
    style T3 fill:#e0f2f1
    style W1 fill:#f1f8e9
    style W2 fill:#f1f8e9
    style DedupeAgent fill:#fff9c4
    style VectorOps fill:#e1bee7
```

### Simplified Architecture Diagram (Miro-Compatible)

```mermaid
flowchart TB
    Client[Flutter Web Client] --> API[ASP.NET Core API]
    API --> CORS[CORS Middleware]
    CORS --> MediatR[MediatR CQRS]
    
    MediatR --> Commands[Commands: Write Operations]
    MediatR --> Queries[Queries: Read Operations]
    
    Commands --> Handlers[Command Handlers]
    Queries --> QHandlers[Query Handlers]
    
    Handlers --> Entities[Domain Entities]
    QHandlers --> Entities
    
    Entities --> Repos[Repositories]
    Repos --> DB[(PostgreSQL + pgvector)]
    
    Handlers --> AIService[Azure OpenAI Service]
    QHandlers --> AIService
    
    AIService --> GPT4[GPT-4o Model]
    AIService --> Embeddings[text-embedding-3-large]
    
    Workers[Background Workers] --> Repos
    Workers --> AIService
    
    GPT4 --> SentimentAnalysis[Sentiment Analysis]
    GPT4 --> PriorityCalc[Priority Calculation]
    
    Embeddings --> VectorSearch[Vector Search]
    VectorSearch --> DB
    
    DB --> Tables[Tables: domains, features, requests, feedback, votes]
    
    style Client fill:#e1f5ff
    style API fill:#fff4e1
    style MediatR fill:#e8f5e9
    style Entities fill:#f3e5f5
    style Repos fill:#fce4ec
    style AIService fill:#fff3e0
    style GPT4 fill:#ffe0b2
    style Embeddings fill:#ffe0b2
    style DB fill:#e0f2f1
    style Workers fill:#f1f8e9
```

## Feature Request Submission - Detailed Flow

```mermaid
sequenceDiagram
    participant Client
    participant API as API Controller
    participant MediatR
    participant Handler as SubmitFeatureRequestCommandHandler
    participant Entity as FeatureRequest Entity
    participant Agent as DeduplicationAgent
    participant AIService as AzureOpenAIService
    participant Azure as Azure OpenAI (text-embedding-3-large)
    participant Repo as FeatureRequestRepository
    participant DB as PostgreSQL + pgvector
    participant Worker as FeatureRequestProcessorWorker

    Client->>API: POST /api/feature-requests<br/>{title, description, requester}
    API->>API: Validate request body
    API->>MediatR: Send(SubmitFeatureRequestCommand)
    MediatR->>MediatR: Run ValidationBehavior
    MediatR->>Handler: Handle(command)
    
    Handler->>Entity: Create new FeatureRequest
    Entity->>Entity: Validate business rules<br/>- Title not empty<br/>- Description length
    Entity-->>Handler: Valid entity
    
    Handler->>Agent: CheckForDuplicatesAsync(title, description)
    Agent->>AIService: GenerateEmbeddingAsync(title + description)
    AIService->>Azure: POST /openai/deployments/text-embedding-3-large/embeddings
    Azure->>Azure: Generate 1536-dim vector
    Azure-->>AIService: Return embedding array
    AIService-->>Agent: float[] embedding
    
    Agent->>Repo: FindSimilarAsync(embedding, threshold: 0.85, limit: 5)
    Repo->>DB: SELECT * FROM fn_feature_request_find_similar($1, $2, $3)
    DB->>DB: Calculate cosine distance<br/>embedding_vector <=> $1<br/>ORDER BY distance<br/>LIMIT $3
    DB-->>Repo: Similar requests (if any)
    Repo-->>Agent: List of similar requests with scores
    
    alt Duplicate Found (score >= 0.85)
        Agent-->>Handler: Throw InvalidOperationException<br/>"Duplicate found: {existingId}"
        Handler-->>MediatR: Exception
        MediatR-->>API: ValidationException
        API-->>Client: HTTP 400 Bad Request<br/>{error: "Duplicate request exists"}
    else No Duplicate
        Agent-->>Handler: No duplicates found
        Handler->>Entity: Set initial status = Pending
        Handler->>Repo: AddAsync(entity)
        Repo->>DB: INSERT INTO feature_requests<br/>(id, title, description, status, created_at)<br/>VALUES (...)
        DB->>DB: Store row (embedding_vector = NULL initially)
        DB-->>Repo: Return inserted entity
        Repo-->>Handler: FeatureRequest with ID
        Handler->>Handler: Map to FeatureRequestDto
        Handler-->>MediatR: FeatureRequestDto
        MediatR-->>API: Result
        API-->>Client: HTTP 201 Created<br/>{id, title, status: "Pending", ...}
    end
    
    Note over Worker: Background processing (every 30s)
    Worker->>Worker: ExecuteAsync triggered
    Worker->>Repo: GetPendingAsync() WHERE embedding_vector IS NULL
    Repo->>DB: SELECT * FROM feature_requests<br/>WHERE embedding_vector IS NULL<br/>LIMIT 10
    DB-->>Repo: Unprocessed requests
    Repo-->>Worker: List of requests
    
    loop For each request
        Worker->>AIService: GenerateEmbeddingAsync(title + description)
        AIService->>Azure: POST /openai/deployments/text-embedding-3-large/embeddings
        Azure-->>AIService: float[] embedding
        AIService-->>Worker: Embedding vector
        Worker->>Repo: UpdateEmbeddingAsync(requestId, embedding)
        Repo->>DB: UPDATE feature_requests<br/>SET embedding_vector = $1<br/>WHERE id = $2
        DB-->>Repo: Success
        Repo-->>Worker: Updated
    end
```

## Feedback Submission with AI Sentiment Analysis

```mermaid
sequenceDiagram
    participant Client
    participant API as FeedbackController
    participant MediatR
    participant Handler as SubmitFeedbackCommandHandler (Infrastructure)
    participant AIService as AzureOpenAIService
    participant Azure as Azure OpenAI (GPT-4o)
    participant Repo as FeedbackRepository
    participant DB as PostgreSQL

    Client->>API: POST /api/feedback<br/>{content, featureId, userId}
    API->>MediatR: Send(SubmitFeedbackCommand)
    MediatR->>Handler: Handle(command)
    
    Handler->>Handler: Build AI prompt<br/>"Analyze sentiment of this feedback..."
    Handler->>AIService: CompleteChatAsync(messages, temperature: 0.1)
    
    AIService->>Azure: POST /openai/deployments/gpt-4o/chat/completions<br/>messages: [<br/>  SystemChatMessage("You are a sentiment analyzer..."),<br/>  UserChatMessage("Analyze: {content}")<br/>]
    
    Azure->>Azure: GPT-4o processes<br/>- Understands context<br/>- Analyzes tone<br/>- Determines sentiment
    
    Azure-->>AIService: Response: {<br/>  sentiment: "Positive",<br/>  confidence: 0.92,<br/>  reasoning: "User expresses satisfaction..."<br/>}
    
    AIService->>AIService: Parse JSON response
    AIService-->>Handler: sentiment: Positive, confidence: 0.92
    
    Handler->>Handler: Create Feedback entity<br/>- Set content<br/>- Set sentiment (AI-generated)<br/>- Set confidence score
    
    Handler->>AIService: GenerateEmbeddingAsync(content)
    AIService->>Azure: POST /openai/deployments/text-embedding-3-large/embeddings
    Azure-->>AIService: float[] embedding
    AIService-->>Handler: Embedding vector
    
    Handler->>Repo: AddAsync(feedback)
    Repo->>DB: INSERT INTO feedback<br/>(content, sentiment, confidence, embedding_vector, ...)
    DB-->>Repo: Inserted feedback
    Repo-->>Handler: Feedback entity
    
    Handler->>Handler: Map to FeedbackDto
    Handler-->>MediatR: FeedbackDto
    MediatR-->>API: Result
    API-->>Client: HTTP 201 Created<br/>{id, sentiment: "Positive", confidence: 0.92}
```

## Semantic Search Flow

```mermaid
sequenceDiagram
    participant Client
    participant API as SearchController
    participant MediatR
    participant Handler as SearchFeaturesQueryHandler
    participant AIService as AzureOpenAIService
    participant Azure as Azure OpenAI
    participant FeatureRepo as FeatureRepository
    participant DomainRepo as DomainRepository
    participant DB as PostgreSQL + pgvector

    Client->>API: GET /api/search/features?q="dark mode"&threshold=0.7&limit=20
    API->>API: Validate parameters<br/>- q is required<br/>- threshold: 0-1<br/>- limit: 1-100
    API->>MediatR: Send(SearchFeaturesQuery)
    MediatR->>Handler: Handle(query)
    
    Handler->>AIService: GenerateEmbeddingAsync("dark mode")
    AIService->>Azure: POST /openai/deployments/text-embedding-3-large/embeddings<br/>{input: "dark mode"}
    Azure->>Azure: Generate embedding<br/>1536-dimensional vector<br/>representing semantic meaning
    Azure-->>AIService: float[1536] embedding
    AIService-->>Handler: Query embedding
    
    Handler->>FeatureRepo: FindSimilarAsync(embedding, threshold: 0.7, limit: 20)
    FeatureRepo->>DB: SELECT * FROM fn_feature_find_similar(<br/>  $1::vector,  -- query embedding<br/>  $2,          -- 0.7 threshold<br/>  $3           -- 20 limit<br/>)
    
    DB->>DB: Execute vector search<br/>WITH similarities AS (<br/>  SELECT f.*, <br/>    1 - (f.embedding_vector <=> $1) as similarity<br/>  FROM features f<br/>  WHERE f.embedding_vector IS NOT NULL<br/>)<br/>SELECT * FROM similarities<br/>WHERE similarity >= $2<br/>ORDER BY similarity DESC<br/>LIMIT $3
    
    DB-->>FeatureRepo: List of similar features<br/>[{id, title, similarity: 0.89}, ...]
    FeatureRepo-->>Handler: Features with similarity scores
    
    Handler->>Handler: Extract domain IDs<br/>Distinct domain IDs from features
    
    par Load domains in parallel
        Handler->>DomainRepo: GetByIdAsync(domainId1)
        Handler->>DomainRepo: GetByIdAsync(domainId2)
        Handler->>DomainRepo: GetByIdAsync(domainId3)
    end
    
    DomainRepo-->>Handler: Task.WhenAll() results
    
    Handler->>Handler: Map to SearchResultDto<br/>For each feature:<br/>- Create FeatureSearchResult<br/>- Set SimilarityScore<br/>- Set DomainName<br/>- Include all metadata
    
    Handler-->>MediatR: List<SearchResult<FeatureSearchResult>>
    MediatR-->>API: Search results
    API-->>Client: HTTP 200 OK<br/>[<br/>  {<br/>    id: "...",<br/>    title: "Dark Mode Support",<br/>    similarityScore: 0.89,<br/>    domainName: "UI/UX",<br/>    ...<br/>  },<br/>  ...<br/>]
```

## Priority Calculation Worker Flow

```mermaid
sequenceDiagram
    participant Timer as Timer (Every 6 hours)
    participant Worker as PriorityCalculationWorker
    participant FeatureRepo as FeatureRepository
    participant DB as PostgreSQL
    participant AIService as AzureOpenAIService
    participant Azure as Azure OpenAI (GPT-4o)

    Timer->>Worker: ExecuteAsync() triggered
    Worker->>Worker: Log: "Recalculating priorities..."
    
    Worker->>FeatureRepo: GetAllAsync()
    FeatureRepo->>DB: SELECT * FROM features
    DB-->>FeatureRepo: All features
    FeatureRepo-->>Worker: List of features
    
    Worker->>Worker: Filter active features<br/>WHERE status IN (Accepted, InProgress)<br/>ORDER BY updated_at ASC<br/>TAKE 20 (batch size)
    
    loop For each feature in batch
        Worker->>Worker: Build AI prompt<br/>"Analyze this feature and provide priority score (0.00 to 1.00):<br/>Title: {feature.title}<br/>Description: {feature.description}<br/>Current Priority: {feature.priority}<br/>Estimated Effort: {feature.estimatedEffortPoints}<br/>Business Value: {feature.businessValueScore}<br/>Status: {feature.status}"
        
        Worker->>AIService: CompleteChatAsync(messages, temperature: 0.3, maxTokens: 500)
        AIService->>Azure: POST /openai/deployments/gpt-4o/chat/completions<br/>messages: [<br/>  SystemChatMessage("You are an expert product manager..."),<br/>  UserChatMessage(prompt)<br/>]
        
        Azure->>Azure: GPT-4o analyzes<br/>- Business value vs effort<br/>- Customer impact<br/>- Strategic alignment<br/>- Current priority<br/>- Generates score + reasoning
        
        Azure-->>AIService: Response: {<br/>  "score": 0.75,<br/>  "reasoning": "High business value with moderate effort..."<br/>}
        
        AIService->>AIService: Parse JSON<br/>Deserialize to PriorityResponse
        AIService-->>Worker: score: 0.75, reasoning: "..."
        
        Worker->>Worker: Validate score range<br/>Math.Clamp(score, 0, 1)
        
        alt Score changed significantly (> 0.1 difference)
            Worker->>Worker: Log: "Updating priority for {featureId}<br/>Old: {oldScore} → New: {newScore}"
            Worker->>FeatureRepo: UpdatePriorityAsync(featureId, score, reasoning)
            FeatureRepo->>DB: UPDATE features<br/>SET ai_priority_score = $1,<br/>    ai_priority_reasoning = $2,<br/>    updated_at = NOW()<br/>WHERE id = $3
            DB-->>FeatureRepo: Success
            FeatureRepo-->>Worker: Updated
        else Score unchanged
            Worker->>Worker: Skip update<br/>Log: "Priority stable for {featureId}"
        end
    end
    
    Worker->>Worker: Log: "Priority calculation completed<br/>{updatedCount} updated, {errorCount} errors"
```

## Vote Weight Calculation

```mermaid
flowchart TD
    Start[User votes for feature] --> GetTier[Get voter's customer tier]
    
    GetTier --> CheckTier{Customer Tier?}
    
    CheckTier -->|Enterprise| Weight3[Weight = 3.0<br/>High priority customer]
    CheckTier -->|Professional| Weight2[Weight = 2.0<br/>Medium priority]
    CheckTier -->|Starter| Weight1[Weight = 1.0<br/>Standard weight]
    
    Weight3 --> CreateVote[Create FeatureVote entity<br/>with calculated weight]
    Weight2 --> CreateVote
    Weight1 --> CreateVote
    
    CreateVote --> SaveVote[Save to database<br/>INSERT INTO feature_votes]
    
    SaveVote --> UpdateFeature[Update feature aggregate<br/>Total votes count<br/>Weighted score]
    
    UpdateFeature --> Return[Return vote confirmation<br/>with weight applied]
    
    style Start fill:#e1f5ff
    style Weight3 fill:#c8e6c9
    style Weight2 fill:#fff9c4
    style Weight1 fill:#ffccbc
    style Return fill:#e1bee7
```

## Request Lifecycle State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending: Feature Request Submitted
    
    Pending --> UnderReview: Product Manager Reviews
    Pending --> Duplicate: Marked as Duplicate<br/>(similarityScore >= 0.85)
    
    UnderReview --> Accepted: Approved & Linked to Feature
    UnderReview --> Rejected: Not Feasible/Out of Scope
    
    Accepted --> [*]: Request Fulfilled<br/>Feature Delivered
    
    Rejected --> [*]: Request Closed
    
    Duplicate --> [*]: Linked to Original Request
    
    note right of Pending
        - Embeddings generated (async)
        - Deduplication check
        - Waiting for PM review
    end note
    
    note right of UnderReview
        - Product Manager analyzing
        - Gathering stakeholder input
        - Evaluating feasibility
    end note
    
    note right of Accepted
        - Linked to feature ID
        - Status auto-set on link
        - Tracked in feature backlog
    end note
    
    note right of Duplicate
        - Similar request found
        - Similarity score stored
        - Reference to original ID
    end note
```

## Database Schema with Relationships

```mermaid
erDiagram
    DOMAINS ||--o{ FEATURES : contains
    DOMAINS ||--o{ DOMAIN_GOALS : has
    FEATURES ||--o{ FEATURE_REQUESTS : "linked to"
    FEATURES ||--o{ FEEDBACK : "receives"
    FEATURES ||--o{ FEATURE_VOTES : "receives"
    FEATURE_REQUESTS ||--o{ FEATURE_REQUESTS : "duplicates"
    
    DOMAINS {
        uuid id PK
        uuid parent_id FK
        string name
        string description
        string path
        timestamp created_at
        timestamp updated_at
    }
    
    FEATURES {
        uuid id PK
        uuid domain_id FK
        string title
        text description
        enum status
        enum priority
        int estimated_effort_points
        decimal business_value_score
        decimal ai_priority_score
        text ai_priority_reasoning
        date target_release_date
        timestamp created_at
        timestamp updated_at
    }
    
    FEATURE_REQUESTS {
        uuid id PK
        string title
        text description
        enum status
        enum source
        string source_id
        string requester_name
        string requester_email
        string requester_company
        enum requester_tier
        uuid linked_feature_id FK
        uuid duplicate_of_request_id FK
        decimal similarity_score
        vector embedding_vector
        timestamp processed_at
        timestamp created_at
        timestamp updated_at
    }
    
    FEEDBACK {
        uuid id PK
        uuid feature_id FK
        uuid feature_request_id FK
        uuid user_id
        text content
        enum sentiment
        decimal sentiment_confidence
        vector embedding_vector
        timestamp created_at
    }
    
    FEATURE_VOTES {
        uuid id PK
        uuid feature_id FK
        uuid user_id
        string voter_email
        enum voter_tier
        decimal weight
        timestamp created_at
    }
    
    DOMAIN_GOALS {
        uuid id PK
        uuid domain_id FK
        string goal_description
        int target_quarter
        int target_year
        timestamp created_at
    }
```

## Integration Sequence - Complete Request Lifecycle

```mermaid
sequenceDiagram
    autonumber
    participant User as End User
    participant Client as Flutter Client
    participant API as API Gateway
    participant Handler as Command Handler
    participant Entity as Domain Entity
    participant AIEmbed as Embedding Service
    participant AIChat as Chat Service
    participant Repo as Repository
    participant DB as PostgreSQL
    participant Worker as Background Worker
    participant Azure as Azure OpenAI

    rect rgb(230, 240, 255)
        Note over User,Azure: Phase 1: Request Submission
        User->>Client: Submit feature request
        Client->>API: POST /api/feature-requests
        API->>Handler: SubmitFeatureRequestCommand
        Handler->>Entity: Create entity
        Entity->>Entity: Validate business rules
        Handler->>AIEmbed: Generate embedding
        AIEmbed->>Azure: Call text-embedding-3-large
        Azure-->>AIEmbed: Return vector
        Handler->>Repo: Check duplicates (vector search)
        Repo->>DB: fn_feature_request_find_similar
        DB-->>Repo: No duplicates found
        Handler->>Repo: Save request (status: Pending)
        Repo->>DB: INSERT feature_request
        DB-->>Handler: Request ID
        Handler-->>API: FeatureRequestDto
        API-->>Client: 201 Created
        Client-->>User: Confirmation
    end

    rect rgb(255, 250, 230)
        Note over Worker,Azure: Phase 2: Background Processing
        Worker->>Repo: Poll for unprocessed requests
        Repo->>DB: SELECT WHERE embedding IS NULL
        DB-->>Worker: Requests needing embeddings
        Worker->>AIEmbed: Generate embeddings
        AIEmbed->>Azure: Batch embedding generation
        Azure-->>Worker: Embeddings
        Worker->>Repo: Update embeddings
        Repo->>DB: UPDATE embedding_vector
    end

    rect rgb(240, 255, 240)
        Note over User,DB: Phase 3: Product Manager Review
        User->>Client: Review pending requests
        Client->>API: GET /api/feature-requests?status=Pending
        API->>Repo: Query pending
        Repo->>DB: SELECT * FROM feature_requests
        DB-->>Client: Pending requests list
        User->>Client: Link request to existing feature
        Client->>API: POST /api/feature-requests/{id}/link
        API->>Handler: LinkRequestToFeatureCommand
        Handler->>Entity: entity.LinkToFeature(featureId)
        Entity->>Entity: Status → Accepted
        Handler->>Repo: Update request
        Repo->>DB: UPDATE status, linked_feature_id
        DB-->>Client: Success
    end

    rect rgb(255, 240, 245)
        Note over User,Azure: Phase 4: User Feedback & Sentiment
        User->>Client: Submit feedback on feature
        Client->>API: POST /api/feedback
        API->>Handler: SubmitFeedbackCommand
        Handler->>AIChat: Analyze sentiment
        AIChat->>Azure: Call GPT-4o
        Azure-->>Handler: Sentiment + confidence
        Handler->>Repo: Save feedback with AI sentiment
        Repo->>DB: INSERT feedback
        DB-->>Client: Feedback saved
    end

    rect rgb(245, 240, 255)
        Note over User,DB: Phase 5: User Voting
        User->>Client: Vote for feature
        Client->>API: POST /api/votes
        API->>Handler: VoteForFeatureCommand
        Handler->>Entity: Calculate weight (tier-based)
        Entity->>Entity: Enterprise: 3x, Pro: 2x, Starter: 1x
        Handler->>Repo: Save vote with weight
        Repo->>DB: INSERT feature_vote
        DB-->>Client: Vote recorded
    end

    rect rgb(255, 245, 230)
        Note over Worker,Azure: Phase 6: Priority Recalculation (Every 6h)
        Worker->>Repo: Get active features
        Repo->>DB: SELECT features (Accepted/InProgress)
        DB-->>Worker: Feature list
        Worker->>AIChat: Calculate AI priority
        AIChat->>Azure: Analyze with GPT-4o
        Azure-->>Worker: Priority score + reasoning
        Worker->>Repo: Update feature priority
        Repo->>DB: UPDATE ai_priority_score
    end

    rect rgb(240, 255, 255)
        Note over User,DB: Phase 7: Semantic Search
        User->>Client: Search "dark mode"
        Client->>API: GET /api/search/features?q=dark mode
        API->>AIEmbed: Generate query embedding
        AIEmbed->>Azure: Embed search query
        Azure-->>API: Query vector
        API->>Repo: Vector similarity search
        Repo->>DB: fn_feature_find_similar
        DB-->>Client: Semantically similar features
    end
```

## Technology Stack & Integration Points

```mermaid
graph LR
    subgraph "Frontend"
        Flutter[Flutter/Dart<br/>Web & Mobile]
    end
    
    subgraph "API Layer"
        ASPNET[ASP.NET Core 9.0<br/>Web API]
        Swagger[Swagger/OpenAPI<br/>Documentation]
    end
    
    subgraph "Application"
        MediatR[MediatR<br/>CQRS Pattern]
        FluentVal[FluentValidation<br/>Request Validation]
    end
    
    subgraph "Domain"
        Entities[Domain Entities<br/>Business Logic]
        Enums[Enums & Value Objects]
    end
    
    subgraph "Infrastructure"
        Dapper[Dapper<br/>Micro-ORM]
        Npgsql[Npgsql<br/>PostgreSQL Driver]
        AzureSDK[Azure.AI.OpenAI<br/>SDK]
    end
    
    subgraph "Database"
        Postgres[(PostgreSQL 16)]
        pgvector[pgvector Extension<br/>Vector Similarity]
    end
    
    subgraph "AI Services"
        OpenAI[Azure OpenAI Service]
        GPT4[GPT-4o Model]
        Embed[text-embedding-3-large]
    end
    
    subgraph "Background"
        Workers[.NET Hosted Services<br/>Background Workers]
    end
    
    subgraph "Security"
        CORS[CORS Policy]
        KeyVault[User Secrets<br/>Future: Key Vault]
        PrivateLink[Private Endpoint<br/>Network Isolation]
    end
    
    Flutter -->|HTTP/HTTPS| ASPNET
    ASPNET -->|Routes| MediatR
    MediatR -->|Validates| FluentVal
    MediatR -->|Executes| Entities
    Entities -->|Persists via| Dapper
    Dapper -->|SQL Commands| Npgsql
    Npgsql -->|Connection| Postgres
    Postgres -->|Vector Ops| pgvector
    ASPNET -->|AI Calls| AzureSDK
    AzureSDK -->|Private/Public| PrivateLink
    PrivateLink -->|Secured| OpenAI
    OpenAI -->|Chat| GPT4
    OpenAI -->|Embeddings| Embed
    Workers -->|Scheduled Jobs| Entities
    Workers -->|AI Processing| AzureSDK
    CORS -->|Protects| ASPNET
    KeyVault -->|Secrets| AzureSDK
    Swagger -->|Documents| ASPNET
```

---

## Key Integration Decisions & Rationale

### 1. **CQRS with MediatR**
- **Why**: Separates read and write concerns, improves scalability
- **How**: Commands for writes, Queries for reads, handlers isolated
- **Benefit**: Clear separation, easier testing, better performance

### 2. **Azure OpenAI Private Endpoint**
- **Why**: Enterprise-grade security, data privacy compliance
- **How**: VNet integration, private DNS, public access disabled in prod
- **Benefit**: No internet exposure, GDPR/HIPAA ready, network isolation

### 3. **Background Workers for AI Processing**
- **Why**: AI calls are expensive (time & cost), don't block user requests
- **How**: .NET Hosted Services poll and process asynchronously
- **Benefit**: Fast API responses, batch processing, retry logic

### 4. **pgvector for Semantic Search**
- **Why**: Native vector operations in PostgreSQL, no external service
- **How**: Store embeddings alongside data, cosine distance search
- **Benefit**: Single database, fast queries, no data duplication

### 5. **Tier-Based Vote Weighting**
- **Why**: Prioritize feedback from high-value customers
- **How**: Enterprise 3x, Professional 2x, Starter 1x multiplication
- **Benefit**: Fair prioritization, revenue-aligned decisions

### 6. **AI-Powered Deduplication**
- **Why**: Semantic similarity catches duplicates humans miss
- **How**: Embedding generation + vector similarity (threshold 0.85)
- **Benefit**: Reduces noise, consolidates feedback, better insights

### 7. **Automated Priority Calculation**
- **Why**: Objective, data-driven prioritization at scale
- **How**: GPT-4o analyzes value/effort/impact every 6 hours
- **Benefit**: Consistent scoring, learns from patterns, reduces bias

### 8. **FluentValidation + Domain Validation**
- **Why**: Defense in depth - validate at API and domain layers
- **How**: FluentValidation for structure, entities for business rules
- **Benefit**: Early failure, clear errors, data integrity

### 9. **Dapper Micro-ORM**
- **Why**: Performance, control over SQL, no overhead
- **How**: Direct SQL with parameter binding, stored procs for vectors
- **Benefit**: Fast queries, pgvector support, explicit database design

### 10. **Sentiment Analysis on Feedback**
- **Why**: Understand user satisfaction without manual tagging
- **How**: GPT-4o analyzes tone, context, generates sentiment + confidence
- **Benefit**: Actionable insights, trend analysis, early problem detection
