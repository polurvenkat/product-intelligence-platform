-- Migration: 004_business_context
-- Description: Table for storing chunked business knowledge with vector embeddings for RAG

CREATE TABLE business_context (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id UUID, -- Optional link to original document result
    content TEXT NOT NULL,
    chunk_index INT NOT NULL,
    metadata JSONB, -- Store origin, file name, etc.
    embedding_vector vector(1536), -- Azure OpenAI text-embedding-3-large
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_business_context_embedding_vector ON business_context USING hnsw (embedding_vector vector_cosine_ops);
CREATE INDEX idx_business_context_metadata ON business_context USING gin (metadata);
