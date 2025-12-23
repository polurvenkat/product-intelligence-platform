-- Product Intelligence Platform - Vector Similarity Functions
-- Migration: 002_vector_similarity_functions
-- PostgreSQL functions for semantic search using pgvector

-- Function: Find similar features using vector similarity
-- Uses cosine distance (1 - <=> operator gives similarity)
CREATE OR REPLACE FUNCTION fn_feature_find_similar(
    query_embedding vector(1536),
    similarity_threshold DECIMAL DEFAULT 0.7,
    max_results INT DEFAULT 20
)
RETURNS TABLE (
    id UUID,
    domain_id UUID,
    title VARCHAR(500),
    description TEXT,
    status VARCHAR(50),
    priority VARCHAR(50),
    estimated_effort_points INT,
    business_value_score DECIMAL(5,2),
    ai_priority_score DECIMAL(5,4),
    ai_priority_reasoning TEXT,
    target_release_date DATE,
    created_at TIMESTAMP,
    updated_at TIMESTAMP,
    similarity_score DECIMAL(5,4)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    WITH similarities AS (
        SELECT 
            f.*,
            (1 - (f.embedding_vector <=> query_embedding)) AS similarity
        FROM features f
        WHERE f.embedding_vector IS NOT NULL
    )
    SELECT 
        s.id,
        s.domain_id,
        s.title,
        s.description,
        s.status,
        s.priority,
        s.estimated_effort_points,
        s.business_value_score,
        s.ai_priority_score,
        s.ai_priority_reasoning,
        s.target_release_date,
        s.created_at,
        s.updated_at,
        s.similarity::DECIMAL(5,4) AS similarity_score
    FROM similarities s
    WHERE s.similarity >= similarity_threshold
    ORDER BY s.similarity DESC
    LIMIT max_results;
END;
$$;

-- Function: Find similar feature requests using vector similarity
-- Includes additional request-specific fields
CREATE OR REPLACE FUNCTION fn_feature_request_find_similar(
    query_embedding vector(1536),
    similarity_threshold DECIMAL DEFAULT 0.7,
    max_results INT DEFAULT 20
)
RETURNS TABLE (
    id UUID,
    title VARCHAR(500),
    description TEXT,
    status VARCHAR(50),
    source VARCHAR(50),
    source_id VARCHAR(200),
    requester_name VARCHAR(200),
    requester_email VARCHAR(320),
    requester_company VARCHAR(200),
    requester_tier VARCHAR(50),
    linked_feature_id UUID,
    duplicate_of_request_id UUID,
    similarity_score DECIMAL(5,4),
    processed_at TIMESTAMP,
    submitted_at TIMESTAMP,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    WITH similarities AS (
        SELECT 
            fr.*,
            (1 - (fr.embedding_vector <=> query_embedding)) AS similarity
        FROM feature_requests fr
        WHERE fr.embedding_vector IS NOT NULL
    )
    SELECT 
        s.id,
        s.title,
        s.description,
        s.status,
        s.source,
        s.source_id,
        s.requester_name,
        s.requester_email,
        s.requester_company,
        s.requester_tier,
        s.linked_feature_id,
        s.duplicate_of_request_id,
        s.similarity::DECIMAL(5,4) AS similarity_score,
        s.processed_at,
        s.submitted_at,
        s.created_at,
        s.updated_at
    FROM similarities s
    WHERE s.similarity >= similarity_threshold
    ORDER BY s.similarity DESC
    LIMIT max_results;
END;
$$;

-- Function: Find similar feedback using vector similarity
-- Useful for grouping related feedback
CREATE OR REPLACE FUNCTION fn_feedback_find_similar(
    query_embedding vector(1536),
    similarity_threshold DECIMAL DEFAULT 0.7,
    max_results INT DEFAULT 20
)
RETURNS TABLE (
    id UUID,
    feature_id UUID,
    feature_request_id UUID,
    user_id UUID,
    content TEXT,
    sentiment VARCHAR(50),
    sentiment_confidence DECIMAL(5,4),
    created_at TIMESTAMP,
    similarity_score DECIMAL(5,4)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    WITH similarities AS (
        SELECT 
            fb.*,
            (1 - (fb.embedding_vector <=> query_embedding)) AS similarity
        FROM feedback fb
        WHERE fb.embedding_vector IS NOT NULL
    )
    SELECT 
        s.id,
        s.feature_id,
        s.feature_request_id,
        s.user_id,
        s.content,
        s.sentiment,
        s.sentiment_confidence,
        s.created_at,
        s.similarity::DECIMAL(5,4) AS similarity_score
    FROM similarities s
    WHERE s.similarity >= similarity_threshold
    ORDER BY s.similarity DESC
    LIMIT max_results;
END;
$$;

-- Function: Get feature vote statistics
-- Aggregates votes with tier-based weighting
CREATE OR REPLACE FUNCTION fn_feature_vote_stats(feature_uuid UUID)
RETURNS TABLE (
    total_votes BIGINT,
    weighted_score DECIMAL(10,2),
    enterprise_votes BIGINT,
    professional_votes BIGINT,
    starter_votes BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*)::BIGINT AS total_votes,
        SUM(weight)::DECIMAL(10,2) AS weighted_score,
        COUNT(*) FILTER (WHERE voter_tier = 'Enterprise')::BIGINT AS enterprise_votes,
        COUNT(*) FILTER (WHERE voter_tier = 'Professional')::BIGINT AS professional_votes,
        COUNT(*) FILTER (WHERE voter_tier = 'Starter')::BIGINT AS starter_votes
    FROM feature_votes
    WHERE feature_id = feature_uuid;
END;
$$;

-- Function: Get domain hierarchy path
-- Recursively builds full path for a domain
CREATE OR REPLACE FUNCTION fn_domain_get_path(domain_uuid UUID)
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
    domain_path TEXT;
BEGIN
    WITH RECURSIVE domain_tree AS (
        SELECT id, name, parent_id, name AS path
        FROM domains
        WHERE id = domain_uuid
        
        UNION ALL
        
        SELECT d.id, d.name, d.parent_id, d.name || ' > ' || dt.path
        FROM domains d
        INNER JOIN domain_tree dt ON dt.parent_id = d.id
    )
    SELECT path INTO domain_path
    FROM domain_tree
    WHERE parent_id IS NULL;
    
    RETURN COALESCE(domain_path, '');
END;
$$;

-- Comment functions for documentation
COMMENT ON FUNCTION fn_feature_find_similar IS 'Finds similar features using vector cosine similarity. Returns features above the similarity threshold, ordered by relevance.';
COMMENT ON FUNCTION fn_feature_request_find_similar IS 'Finds similar feature requests using vector cosine similarity. Used for duplicate detection and request grouping.';
COMMENT ON FUNCTION fn_feedback_find_similar IS 'Finds similar feedback entries using vector cosine similarity. Useful for sentiment trend analysis.';
COMMENT ON FUNCTION fn_feature_vote_stats IS 'Calculates vote statistics for a feature including tier-based weighting (Enterprise: 3x, Professional: 2x, Starter: 1x).';
COMMENT ON FUNCTION fn_domain_get_path IS 'Recursively builds the full hierarchical path for a domain from root to leaf.';
