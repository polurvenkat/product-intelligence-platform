-- Product Intelligence Platform - Initial Schema
-- PostgreSQL 16 with pgvector extension
-- Migration: 001_initial_schema

-- Enable pgvector extension for vector similarity search
-- Note: In managed environments like Azure Database for PostgreSQL, these must be created by an administrator.
-- CREATE EXTENSION IF NOT EXISTS vector;
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Table: domains
-- Hierarchical structure for product areas
CREATE TABLE domains (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    parent_id UUID REFERENCES domains(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    path VARCHAR(1000) NOT NULL, -- Materialized path for hierarchy traversal
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_name_not_empty CHECK (TRIM(name) <> ''),
    CONSTRAINT chk_path_not_empty CHECK (TRIM(path) <> '')
);

-- Table: domain_goals
-- Strategic goals for each domain
CREATE TABLE domain_goals (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    domain_id UUID NOT NULL REFERENCES domains(id) ON DELETE CASCADE,
    goal_description TEXT NOT NULL,
    target_quarter INT NOT NULL CHECK (target_quarter BETWEEN 1 AND 4),
    target_year INT NOT NULL CHECK (target_year >= 2024),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_goal_not_empty CHECK (TRIM(goal_description) <> '')
);

-- Table: features
-- Product features within domains
CREATE TABLE features (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    domain_id UUID NOT NULL REFERENCES domains(id) ON DELETE CASCADE,
    title VARCHAR(500) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Proposed',
    priority VARCHAR(50) NOT NULL DEFAULT 'Medium',
    estimated_effort_points INT CHECK (estimated_effort_points >= 0),
    business_value_score DECIMAL(5,2) CHECK (business_value_score BETWEEN 0 AND 100),
    ai_priority_score DECIMAL(5,4) CHECK (ai_priority_score BETWEEN 0 AND 1),
    ai_priority_reasoning TEXT,
    target_release_date DATE,
    embedding_vector vector(1536), -- Azure OpenAI text-embedding-3-large
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_title_not_empty CHECK (TRIM(title) <> ''),
    CONSTRAINT chk_description_not_empty CHECK (TRIM(description) <> ''),
    CONSTRAINT chk_status_valid CHECK (status IN ('Proposed', 'UnderReview', 'Accepted', 'InProgress', 'Completed', 'OnHold', 'Rejected')),
    CONSTRAINT chk_priority_valid CHECK (priority IN ('Low', 'Medium', 'High', 'Critical'))
);

-- Table: feature_requests
-- Customer feature requests from multiple sources
CREATE TABLE feature_requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(500) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    source VARCHAR(50) NOT NULL DEFAULT 'Manual',
    source_id VARCHAR(200),
    requester_name VARCHAR(200) NOT NULL,
    requester_email VARCHAR(320),
    requester_company VARCHAR(200),
    requester_tier VARCHAR(50) NOT NULL DEFAULT 'Starter',
    linked_feature_id UUID REFERENCES features(id) ON DELETE SET NULL,
    duplicate_of_request_id UUID REFERENCES feature_requests(id) ON DELETE SET NULL,
    similarity_score DECIMAL(5,4) CHECK (similarity_score BETWEEN 0 AND 1),
    embedding_vector vector(1536), -- Azure OpenAI text-embedding-3-large
    processed_at TIMESTAMP,
    submitted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_title_not_empty CHECK (TRIM(title) <> ''),
    CONSTRAINT chk_description_not_empty CHECK (TRIM(description) <> ''),
    CONSTRAINT chk_requester_name_not_empty CHECK (TRIM(requester_name) <> ''),
    CONSTRAINT chk_status_valid CHECK (status IN ('Pending', 'UnderReview', 'Accepted', 'Rejected', 'Duplicate')),
    CONSTRAINT chk_source_valid CHECK (source IN ('Manual', 'API', 'Slack', 'Email', 'SupportTicket', 'UserPortal')),
    CONSTRAINT chk_tier_valid CHECK (requester_tier IN ('Starter', 'Professional', 'Enterprise'))
);

-- Table: feedback
-- User feedback on features
CREATE TABLE feedback (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    feature_id UUID REFERENCES features(id) ON DELETE CASCADE,
    feature_request_id UUID REFERENCES feature_requests(id) ON DELETE CASCADE,
    user_id UUID, -- Future: link to user table
    content TEXT NOT NULL,
    sentiment VARCHAR(50),
    sentiment_confidence DECIMAL(5,4) CHECK (sentiment_confidence BETWEEN 0 AND 1),
    embedding_vector vector(1536), -- Azure OpenAI text-embedding-3-large
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_content_not_empty CHECK (TRIM(content) <> ''),
    CONSTRAINT chk_sentiment_valid CHECK (sentiment IN ('Positive', 'Neutral', 'Negative', 'Mixed')),
    CONSTRAINT chk_either_feature_or_request CHECK (
        (feature_id IS NOT NULL AND feature_request_id IS NULL) OR
        (feature_id IS NULL AND feature_request_id IS NOT NULL)
    )
);

-- Table: feature_votes
-- Customer votes for features with tier-based weighting
CREATE TABLE feature_votes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    feature_id UUID NOT NULL REFERENCES features(id) ON DELETE CASCADE,
    user_id UUID, -- Future: link to user table
    voter_email VARCHAR(320) NOT NULL,
    voter_tier VARCHAR(50) NOT NULL DEFAULT 'Starter',
    weight DECIMAL(3,1) NOT NULL DEFAULT 1.0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_voter_email_not_empty CHECK (TRIM(voter_email) <> ''),
    CONSTRAINT chk_tier_valid CHECK (voter_tier IN ('Starter', 'Professional', 'Enterprise')),
    CONSTRAINT chk_weight_valid CHECK (weight BETWEEN 1.0 AND 3.0),
    CONSTRAINT uq_feature_voter UNIQUE (feature_id, voter_email)
);

-- Indexes for performance
CREATE INDEX idx_domains_parent_id ON domains(parent_id);
CREATE INDEX idx_domains_path ON domains USING btree(path);
CREATE INDEX idx_domain_goals_domain_id ON domain_goals(domain_id);
CREATE INDEX idx_features_domain_id ON features(domain_id);
CREATE INDEX idx_features_status ON features(status);
CREATE INDEX idx_features_priority ON features(priority);
CREATE INDEX idx_features_created_at ON features(created_at DESC);
CREATE INDEX idx_features_ai_priority_score ON features(ai_priority_score DESC NULLS LAST);
CREATE INDEX idx_feature_requests_status ON feature_requests(status);
CREATE INDEX idx_feature_requests_linked_feature ON feature_requests(linked_feature_id);
CREATE INDEX idx_feature_requests_duplicate_of ON feature_requests(duplicate_of_request_id);
CREATE INDEX idx_feature_requests_submitted_at ON feature_requests(submitted_at DESC);
CREATE INDEX idx_feature_requests_requester_email ON feature_requests(requester_email);
CREATE INDEX idx_feedback_feature_id ON feedback(feature_id);
CREATE INDEX idx_feedback_feature_request_id ON feedback(feature_request_id);
CREATE INDEX idx_feedback_sentiment ON feedback(sentiment);
CREATE INDEX idx_feedback_created_at ON feedback(created_at DESC);
CREATE INDEX idx_feature_votes_feature_id ON feature_votes(feature_id);
CREATE INDEX idx_feature_votes_voter_email ON feature_votes(voter_email);

-- Vector similarity indexes using HNSW algorithm
CREATE INDEX idx_features_embedding_vector ON features USING hnsw (embedding_vector vector_cosine_ops);
CREATE INDEX idx_feature_requests_embedding_vector ON feature_requests USING hnsw (embedding_vector vector_cosine_ops);
CREATE INDEX idx_feedback_embedding_vector ON feedback USING hnsw (embedding_vector vector_cosine_ops);

-- Trigger function to auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply update triggers
CREATE TRIGGER update_domains_updated_at BEFORE UPDATE ON domains
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_features_updated_at BEFORE UPDATE ON features
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_feature_requests_updated_at BEFORE UPDATE ON feature_requests
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
