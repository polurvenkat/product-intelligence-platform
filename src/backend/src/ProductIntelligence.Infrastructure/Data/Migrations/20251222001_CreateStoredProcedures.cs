using FluentMigrator;

namespace ProductIntelligence.Infrastructure.Data.Migrations;

[Migration(20251222001)]
public class CreateStoredProcedures : Migration
{
    public override void Up()
    {
        // Ensure extensions are created (Note: In Azure, these must be created by an administrator)
        // Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"ltree\";");
        // Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"vector\";");
        // Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

        // Domain Functions
        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_add(
    p_id UUID,
    p_organization_id UUID,
    p_parent_domain_id UUID,
    p_name VARCHAR(200),
    p_description TEXT,
    p_path LTREE,
    p_owner_user_id UUID,
    p_created_at TIMESTAMP,
    p_updated_at TIMESTAMP,
    p_metadata JSONB
) RETURNS UUID AS $$
BEGIN
    INSERT INTO domains (id, organization_id, parent_domain_id, name, description, path, owner_user_id, created_at, updated_at, metadata)
    VALUES (p_id, p_organization_id, p_parent_domain_id, p_name, p_description, p_path, p_owner_user_id, p_created_at, p_updated_at, p_metadata);
    RETURN p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_get_by_id(p_id UUID)
RETURNS TABLE (
    id UUID, organization_id UUID, parent_domain_id UUID, name VARCHAR(200),
    description TEXT, path LTREE, owner_user_id UUID, created_at TIMESTAMP,
    updated_at TIMESTAMP, metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT d.id, d.organization_id, d.parent_domain_id, d.name, d.description,
           d.path, d.owner_user_id, d.created_at, d.updated_at, d.metadata
    FROM domains d
    WHERE d.id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_get_all()
RETURNS TABLE (
    id UUID, organization_id UUID, parent_domain_id UUID, name VARCHAR(200),
    description TEXT, path LTREE, owner_user_id UUID, created_at TIMESTAMP,
    updated_at TIMESTAMP, metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT d.id, d.organization_id, d.parent_domain_id, d.name, d.description,
           d.path, d.owner_user_id, d.created_at, d.updated_at, d.metadata
    FROM domains d
    ORDER BY d.path;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_get_by_organization(p_organization_id UUID)
RETURNS TABLE (
    id UUID, organization_id UUID, parent_domain_id UUID, name VARCHAR(200),
    description TEXT, path LTREE, owner_user_id UUID, created_at TIMESTAMP,
    updated_at TIMESTAMP, metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT d.id, d.organization_id, d.parent_domain_id, d.name, d.description,
           d.path, d.owner_user_id, d.created_at, d.updated_at, d.metadata
    FROM domains d
    WHERE d.organization_id = p_organization_id
    ORDER BY d.path;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_get_hierarchy(p_organization_id UUID)
RETURNS TABLE (
    id UUID, organization_id UUID, parent_domain_id UUID, name VARCHAR(200),
    description TEXT, path LTREE, owner_user_id UUID, created_at TIMESTAMP,
    updated_at TIMESTAMP, metadata JSONB, feature_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT d.id, d.organization_id, d.parent_domain_id, d.name, d.description,
           d.path, d.owner_user_id, d.created_at, d.updated_at, d.metadata,
           COUNT(f.id) as feature_count
    FROM domains d
    LEFT JOIN features f ON f.domain_id = d.id
    WHERE d.organization_id = p_organization_id
    GROUP BY d.id, d.organization_id, d.parent_domain_id, d.name, d.description,
             d.path, d.owner_user_id, d.created_at, d.updated_at, d.metadata
    ORDER BY d.path;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_update(
    p_id UUID,
    p_name VARCHAR(200),
    p_description TEXT,
    p_owner_user_id UUID,
    p_updated_at TIMESTAMP,
    p_metadata JSONB
) RETURNS VOID AS $$
BEGIN
    UPDATE domains
    SET name = p_name,
        description = p_description,
        owner_user_id = p_owner_user_id,
        updated_at = p_updated_at,
        metadata = p_metadata
    WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_delete(p_id UUID)
RETURNS VOID AS $$
BEGIN
    DELETE FROM domains WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_exists(p_id UUID)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS(SELECT 1 FROM domains WHERE id = p_id);
END;
$$ LANGUAGE plpgsql;
");

        // Feature Functions
        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_add(
    p_id UUID,
    p_domain_id UUID,
    p_parent_feature_id UUID,
    p_title VARCHAR(500),
    p_description TEXT,
    p_status VARCHAR(50),
    p_priority VARCHAR(10),
    p_ai_priority_score DECIMAL(3,2),
    p_ai_priority_reasoning TEXT,
    p_estimated_effort_points INTEGER,
    p_business_value_score DECIMAL(3,2),
    p_created_by UUID,
    p_created_at TIMESTAMP,
    p_updated_at TIMESTAMP,
    p_target_release VARCHAR(100),
    p_metadata JSONB
) RETURNS UUID AS $$
BEGIN
    INSERT INTO features (id, domain_id, parent_feature_id, title, description, status, priority,
                         ai_priority_score, ai_priority_reasoning, estimated_effort_points,
                         business_value_score, created_by, created_at, updated_at, target_release, metadata)
    VALUES (p_id, p_domain_id, p_parent_feature_id, p_title, p_description, p_status, p_priority,
            p_ai_priority_score, p_ai_priority_reasoning, p_estimated_effort_points,
            p_business_value_score, p_created_by, p_created_at, p_updated_at, p_target_release, p_metadata);
    RETURN p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_get_by_id(p_id UUID)
RETURNS TABLE (
    id UUID, domain_id UUID, parent_feature_id UUID, title VARCHAR(500),
    description TEXT, status VARCHAR(50), priority VARCHAR(10),
    ai_priority_score DECIMAL(3,2), ai_priority_reasoning TEXT,
    estimated_effort_points INTEGER, business_value_score DECIMAL(3,2),
    created_by UUID, created_at TIMESTAMP, updated_at TIMESTAMP,
    target_release VARCHAR(100), metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.domain_id, f.parent_feature_id, f.title, f.description,
           f.status, f.priority, f.ai_priority_score, f.ai_priority_reasoning,
           f.estimated_effort_points, f.business_value_score, f.created_by,
           f.created_at, f.updated_at, f.target_release, f.metadata
    FROM features f
    WHERE f.id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_get_all()
RETURNS TABLE (
    id UUID, domain_id UUID, parent_feature_id UUID, title VARCHAR(500),
    description TEXT, status VARCHAR(50), priority VARCHAR(10),
    ai_priority_score DECIMAL(3,2), ai_priority_reasoning TEXT,
    estimated_effort_points INTEGER, business_value_score DECIMAL(3,2),
    created_by UUID, created_at TIMESTAMP, updated_at TIMESTAMP,
    target_release VARCHAR(100), metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.domain_id, f.parent_feature_id, f.title, f.description,
           f.status, f.priority, f.ai_priority_score, f.ai_priority_reasoning,
           f.estimated_effort_points, f.business_value_score, f.created_by,
           f.created_at, f.updated_at, f.target_release, f.metadata
    FROM features f
    ORDER BY f.created_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_get_by_domain(p_domain_id UUID)
RETURNS TABLE (
    id UUID, domain_id UUID, parent_feature_id UUID, title VARCHAR(500),
    description TEXT, status VARCHAR(50), priority VARCHAR(10),
    ai_priority_score DECIMAL(3,2), ai_priority_reasoning TEXT,
    estimated_effort_points INTEGER, business_value_score DECIMAL(3,2),
    created_by UUID, created_at TIMESTAMP, updated_at TIMESTAMP,
    target_release VARCHAR(100), metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.domain_id, f.parent_feature_id, f.title, f.description,
           f.status, f.priority, f.ai_priority_score, f.ai_priority_reasoning,
           f.estimated_effort_points, f.business_value_score, f.created_by,
           f.created_at, f.updated_at, f.target_release, f.metadata
    FROM features f
    WHERE f.domain_id = p_domain_id
    ORDER BY f.ai_priority_score DESC, f.created_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_get_by_status(p_status VARCHAR(50))
RETURNS TABLE (
    id UUID, domain_id UUID, parent_feature_id UUID, title VARCHAR(500),
    description TEXT, status VARCHAR(50), priority VARCHAR(10),
    ai_priority_score DECIMAL(3,2), ai_priority_reasoning TEXT,
    estimated_effort_points INTEGER, business_value_score DECIMAL(3,2),
    created_by UUID, created_at TIMESTAMP, updated_at TIMESTAMP,
    target_release VARCHAR(100), metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.domain_id, f.parent_feature_id, f.title, f.description,
           f.status, f.priority, f.ai_priority_score, f.ai_priority_reasoning,
           f.estimated_effort_points, f.business_value_score, f.created_by,
           f.created_at, f.updated_at, f.target_release, f.metadata
    FROM features f
    WHERE f.status = p_status
    ORDER BY f.ai_priority_score DESC, f.created_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_get_with_vote_count(p_domain_id UUID)
RETURNS TABLE (
    id UUID, domain_id UUID, parent_feature_id UUID, title VARCHAR(500),
    description TEXT, status VARCHAR(50), priority VARCHAR(10),
    ai_priority_score DECIMAL(3,2), ai_priority_reasoning TEXT,
    estimated_effort_points INTEGER, business_value_score DECIMAL(3,2),
    created_by UUID, created_at TIMESTAMP, updated_at TIMESTAMP,
    target_release VARCHAR(100), metadata JSONB, vote_count BIGINT,
    weighted_vote_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.domain_id, f.parent_feature_id, f.title, f.description,
           f.status, f.priority, f.ai_priority_score, f.ai_priority_reasoning,
           f.estimated_effort_points, f.business_value_score, f.created_by,
           f.created_at, f.updated_at, f.target_release, f.metadata,
           COUNT(fv.id) as vote_count,
           COALESCE(SUM(fv.vote_weight), 0) as weighted_vote_count
    FROM features f
    LEFT JOIN feature_votes fv ON fv.feature_id = f.id
    WHERE f.domain_id = p_domain_id
    GROUP BY f.id, f.domain_id, f.parent_feature_id, f.title, f.description,
             f.status, f.priority, f.ai_priority_score, f.ai_priority_reasoning,
             f.estimated_effort_points, f.business_value_score, f.created_by,
             f.created_at, f.updated_at, f.target_release, f.metadata
    ORDER BY weighted_vote_count DESC, f.ai_priority_score DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_update(
    p_id UUID,
    p_title VARCHAR(500),
    p_description TEXT,
    p_status VARCHAR(50),
    p_priority VARCHAR(10),
    p_estimated_effort_points INTEGER,
    p_business_value_score DECIMAL(3,2),
    p_updated_at TIMESTAMP,
    p_target_release VARCHAR(100),
    p_metadata JSONB
) RETURNS VOID AS $$
BEGIN
    UPDATE features
    SET title = p_title,
        description = p_description,
        status = p_status,
        priority = p_priority,
        estimated_effort_points = p_estimated_effort_points,
        business_value_score = p_business_value_score,
        updated_at = p_updated_at,
        target_release = p_target_release,
        metadata = p_metadata
    WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_update_priority(
    p_id UUID,
    p_ai_priority_score DECIMAL(3,2),
    p_ai_priority_reasoning TEXT,
    p_updated_at TIMESTAMP
) RETURNS VOID AS $$
BEGIN
    UPDATE features
    SET ai_priority_score = p_ai_priority_score,
        ai_priority_reasoning = p_ai_priority_reasoning,
        updated_at = p_updated_at
    WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_delete(p_id UUID)
RETURNS VOID AS $$
BEGIN
    DELETE FROM features WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_exists(p_id UUID)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS(SELECT 1 FROM features WHERE id = p_id);
END;
$$ LANGUAGE plpgsql;
");

        // Feature Request Functions
        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_add(
    p_id UUID,
    p_title VARCHAR(500),
    p_description TEXT,
    p_source VARCHAR(50),
    p_source_id VARCHAR(200),
    p_requester_name VARCHAR(200),
    p_requester_email VARCHAR(200),
    p_requester_company VARCHAR(200),
    p_requester_tier VARCHAR(50),
    p_submitted_at TIMESTAMP,
    p_status VARCHAR(50),
    p_metadata JSONB
) RETURNS UUID AS $$
BEGIN
    INSERT INTO feature_requests (id, title, description, source, source_id, requester_name,
                                  requester_email, requester_company, requester_tier,
                                  submitted_at, status, metadata)
    VALUES (p_id, p_title, p_description, p_source, p_source_id, p_requester_name,
            p_requester_email, p_requester_company, p_requester_tier,
            p_submitted_at, p_status, p_metadata);
    RETURN p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_get_by_id(p_id UUID)
RETURNS TABLE (
    id UUID, title VARCHAR(500), description TEXT, source VARCHAR(50),
    source_id VARCHAR(200), requester_name VARCHAR(200), requester_email VARCHAR(200),
    requester_company VARCHAR(200), requester_tier VARCHAR(50), submitted_at TIMESTAMP,
    status VARCHAR(50), embedding_vector VECTOR(1536), processed_at TIMESTAMP,
    linked_feature_id UUID, duplicate_of_request_id UUID, similarity_score DECIMAL(3,2),
    metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT fr.id, fr.title, fr.description, fr.source, fr.source_id,
           fr.requester_name, fr.requester_email, fr.requester_company,
           fr.requester_tier, fr.submitted_at, fr.status, fr.embedding_vector,
           fr.processed_at, fr.linked_feature_id, fr.duplicate_of_request_id,
           fr.similarity_score, fr.metadata
    FROM feature_requests fr
    WHERE fr.id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_get_all()
RETURNS TABLE (
    id UUID, title VARCHAR(500), description TEXT, source VARCHAR(50),
    source_id VARCHAR(200), requester_name VARCHAR(200), requester_email VARCHAR(200),
    requester_company VARCHAR(200), requester_tier VARCHAR(50), submitted_at TIMESTAMP,
    status VARCHAR(50), embedding_vector VECTOR(1536), processed_at TIMESTAMP,
    linked_feature_id UUID, duplicate_of_request_id UUID, similarity_score DECIMAL(3,2),
    metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT fr.id, fr.title, fr.description, fr.source, fr.source_id,
           fr.requester_name, fr.requester_email, fr.requester_company,
           fr.requester_tier, fr.submitted_at, fr.status, fr.embedding_vector,
           fr.processed_at, fr.linked_feature_id, fr.duplicate_of_request_id,
           fr.similarity_score, fr.metadata
    FROM feature_requests fr
    ORDER BY fr.submitted_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_get_by_status(p_status VARCHAR(50))
RETURNS TABLE (
    id UUID, title VARCHAR(500), description TEXT, source VARCHAR(50),
    source_id VARCHAR(200), requester_name VARCHAR(200), requester_email VARCHAR(200),
    requester_company VARCHAR(200), requester_tier VARCHAR(50), submitted_at TIMESTAMP,
    status VARCHAR(50), embedding_vector VECTOR(1536), processed_at TIMESTAMP,
    linked_feature_id UUID, duplicate_of_request_id UUID, similarity_score DECIMAL(3,2),
    metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT fr.id, fr.title, fr.description, fr.source, fr.source_id,
           fr.requester_name, fr.requester_email, fr.requester_company,
           fr.requester_tier, fr.submitted_at, fr.status, fr.embedding_vector,
           fr.processed_at, fr.linked_feature_id, fr.duplicate_of_request_id,
           fr.similarity_score, fr.metadata
    FROM feature_requests fr
    WHERE fr.status = p_status
    ORDER BY fr.submitted_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_find_similar(
    p_embedding_vector VECTOR(1536),
    p_threshold DECIMAL(3,2),
    p_limit INTEGER
) RETURNS TABLE (
    id UUID, title VARCHAR(500), description TEXT, source VARCHAR(50),
    source_id VARCHAR(200), requester_name VARCHAR(200), requester_email VARCHAR(200),
    requester_company VARCHAR(200), requester_tier VARCHAR(50), submitted_at TIMESTAMP,
    status VARCHAR(50), processed_at TIMESTAMP, linked_feature_id UUID,
    duplicate_of_request_id UUID, similarity_score DECIMAL(3,2),
    metadata JSONB, cosine_similarity DOUBLE PRECISION
) AS $$
BEGIN
    RETURN QUERY
    SELECT fr.id, fr.title, fr.description, fr.source, fr.source_id,
           fr.requester_name, fr.requester_email, fr.requester_company,
           fr.requester_tier, fr.submitted_at, fr.status,
           fr.processed_at, fr.linked_feature_id, fr.duplicate_of_request_id,
           fr.similarity_score, fr.metadata,
           (1 - (fr.embedding_vector <=> p_embedding_vector))::DOUBLE PRECISION as cosine_similarity
    FROM feature_requests fr
    WHERE fr.embedding_vector IS NOT NULL
      AND (1 - (fr.embedding_vector <=> p_embedding_vector)) >= p_threshold
    ORDER BY fr.embedding_vector <=> p_embedding_vector
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_get_by_feature(p_feature_id UUID)
RETURNS TABLE (
    id UUID, title VARCHAR(500), description TEXT, source VARCHAR(50),
    source_id VARCHAR(200), requester_name VARCHAR(200), requester_email VARCHAR(200),
    requester_company VARCHAR(200), requester_tier VARCHAR(50), submitted_at TIMESTAMP,
    status VARCHAR(50), embedding_vector VECTOR(1536), processed_at TIMESTAMP,
    linked_feature_id UUID, duplicate_of_request_id UUID, similarity_score DECIMAL(3,2),
    metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT fr.id, fr.title, fr.description, fr.source, fr.source_id,
           fr.requester_name, fr.requester_email, fr.requester_company,
           fr.requester_tier, fr.submitted_at, fr.status, fr.embedding_vector,
           fr.processed_at, fr.linked_feature_id, fr.duplicate_of_request_id,
           fr.similarity_score, fr.metadata
    FROM feature_requests fr
    WHERE fr.linked_feature_id = p_feature_id
    ORDER BY fr.submitted_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_update_embedding(
    p_id UUID,
    p_embedding_vector VECTOR(1536),
    p_processed_at TIMESTAMP
) RETURNS VOID AS $$
BEGIN
    UPDATE feature_requests
    SET embedding_vector = p_embedding_vector,
        processed_at = p_processed_at
    WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_update(
    p_id UUID,
    p_status VARCHAR(50),
    p_linked_feature_id UUID,
    p_duplicate_of_request_id UUID,
    p_similarity_score DECIMAL(3,2)
) RETURNS VOID AS $$
BEGIN
    UPDATE feature_requests
    SET status = p_status,
        linked_feature_id = p_linked_feature_id,
        duplicate_of_request_id = p_duplicate_of_request_id,
        similarity_score = p_similarity_score
    WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_delete(p_id UUID)
RETURNS VOID AS $$
BEGIN
    DELETE FROM feature_requests WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_request_exists(p_id UUID)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS(SELECT 1 FROM feature_requests WHERE id = p_id);
END;
$$ LANGUAGE plpgsql;
");

        // Feedback Functions
        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feedback_add(
    p_id UUID,
    p_feature_id UUID,
    p_feature_request_id UUID,
    p_content TEXT,
    p_sentiment VARCHAR(20),
    p_source VARCHAR(50),
    p_customer_id VARCHAR(200),
    p_customer_tier VARCHAR(50),
    p_submitted_at TIMESTAMP,
    p_embedding_vector VECTOR(1536)
) RETURNS UUID AS $$
BEGIN
    INSERT INTO feedback (id, feature_id, feature_request_id, content, sentiment,
                         source, customer_id, customer_tier, submitted_at, embedding_vector)
    VALUES (p_id, p_feature_id, p_feature_request_id, p_content, p_sentiment,
            p_source, p_customer_id, p_customer_tier, p_submitted_at, p_embedding_vector);
    RETURN p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feedback_get_by_id(p_id UUID)
RETURNS TABLE (
    id UUID, feature_id UUID, feature_request_id UUID, content TEXT,
    sentiment VARCHAR(20), source VARCHAR(50), customer_id VARCHAR(200),
    customer_tier VARCHAR(50), submitted_at TIMESTAMP, embedding_vector VECTOR(1536)
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.feature_id, f.feature_request_id, f.content, f.sentiment,
           f.source, f.customer_id, f.customer_tier, f.submitted_at, f.embedding_vector
    FROM feedback f
    WHERE f.id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feedback_get_by_feature(p_feature_id UUID)
RETURNS TABLE (
    id UUID, feature_id UUID, feature_request_id UUID, content TEXT,
    sentiment VARCHAR(20), source VARCHAR(50), customer_id VARCHAR(200),
    customer_tier VARCHAR(50), submitted_at TIMESTAMP, embedding_vector VECTOR(1536)
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.feature_id, f.feature_request_id, f.content, f.sentiment,
           f.source, f.customer_id, f.customer_tier, f.submitted_at, f.embedding_vector
    FROM feedback f
    WHERE f.feature_id = p_feature_id
    ORDER BY f.submitted_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feedback_get_by_request(p_feature_request_id UUID)
RETURNS TABLE (
    id UUID, feature_id UUID, feature_request_id UUID, content TEXT,
    sentiment VARCHAR(20), source VARCHAR(50), customer_id VARCHAR(200),
    customer_tier VARCHAR(50), submitted_at TIMESTAMP, embedding_vector VECTOR(1536)
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.feature_id, f.feature_request_id, f.content, f.sentiment,
           f.source, f.customer_id, f.customer_tier, f.submitted_at, f.embedding_vector
    FROM feedback f
    WHERE f.feature_request_id = p_feature_request_id
    ORDER BY f.submitted_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feedback_get_by_sentiment(
    p_feature_id UUID,
    p_sentiment VARCHAR(20)
) RETURNS TABLE (
    id UUID, feature_id UUID, feature_request_id UUID, content TEXT,
    sentiment VARCHAR(20), source VARCHAR(50), customer_id VARCHAR(200),
    customer_tier VARCHAR(50), submitted_at TIMESTAMP, embedding_vector VECTOR(1536)
) AS $$
BEGIN
    RETURN QUERY
    SELECT f.id, f.feature_id, f.feature_request_id, f.content, f.sentiment,
           f.source, f.customer_id, f.customer_tier, f.submitted_at, f.embedding_vector
    FROM feedback f
    WHERE f.feature_id = p_feature_id AND f.sentiment = p_sentiment
    ORDER BY f.submitted_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feedback_delete(p_id UUID)
RETURNS VOID AS $$
BEGIN
    DELETE FROM feedback WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        // Feature Vote Functions
        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_vote_add(
    p_id UUID,
    p_feature_id UUID,
    p_feature_request_id UUID,
    p_voter_email VARCHAR(200),
    p_voter_company VARCHAR(200),
    p_voter_tier VARCHAR(50),
    p_vote_weight INTEGER,
    p_voted_at TIMESTAMP
) RETURNS UUID AS $$
BEGIN
    INSERT INTO feature_votes (id, feature_id, feature_request_id, voter_email,
                              voter_company, voter_tier, vote_weight, voted_at)
    VALUES (p_id, p_feature_id, p_feature_request_id, p_voter_email,
            p_voter_company, p_voter_tier, p_vote_weight, p_voted_at);
    RETURN p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_vote_get_by_feature(p_feature_id UUID)
RETURNS TABLE (
    id UUID, feature_id UUID, feature_request_id UUID, voter_email VARCHAR(200),
    voter_company VARCHAR(200), voter_tier VARCHAR(50), vote_weight INTEGER,
    voted_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT fv.id, fv.feature_id, fv.feature_request_id, fv.voter_email,
           fv.voter_company, fv.voter_tier, fv.vote_weight, fv.voted_at
    FROM feature_votes fv
    WHERE fv.feature_id = p_feature_id
    ORDER BY fv.voted_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_vote_get_count(p_feature_id UUID)
RETURNS TABLE (
    vote_count BIGINT,
    weighted_vote_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT COUNT(fv.id) as vote_count,
           COALESCE(SUM(fv.vote_weight), 0) as weighted_vote_count
    FROM feature_votes fv
    WHERE fv.feature_id = p_feature_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_feature_vote_delete(
    p_feature_id UUID,
    p_voter_email VARCHAR(200)
) RETURNS VOID AS $$
BEGIN
    DELETE FROM feature_votes 
    WHERE feature_id = p_feature_id AND voter_email = p_voter_email;
END;
$$ LANGUAGE plpgsql;
");

        // Domain Goal Functions
        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_goal_add(
    p_id UUID,
    p_domain_id UUID,
    p_goal_description TEXT,
    p_target_quarter VARCHAR(10),
    p_priority INTEGER,
    p_created_at TIMESTAMP
) RETURNS UUID AS $$
BEGIN
    INSERT INTO domain_goals (id, domain_id, goal_description, target_quarter, priority, created_at)
    VALUES (p_id, p_domain_id, p_goal_description, p_target_quarter, p_priority, p_created_at);
    RETURN p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_goal_get_by_domain(p_domain_id UUID)
RETURNS TABLE (
    id UUID, domain_id UUID, goal_description TEXT, target_quarter VARCHAR(10),
    priority INTEGER, created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT dg.id, dg.domain_id, dg.goal_description, dg.target_quarter,
           dg.priority, dg.created_at
    FROM domain_goals dg
    WHERE dg.domain_id = p_domain_id
    ORDER BY dg.priority ASC, dg.created_at DESC;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_goal_update(
    p_id UUID,
    p_goal_description TEXT,
    p_target_quarter VARCHAR(10),
    p_priority INTEGER
) RETURNS VOID AS $$
BEGIN
    UPDATE domain_goals
    SET goal_description = p_goal_description,
        target_quarter = p_target_quarter,
        priority = p_priority
    WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");

        Execute.Sql(@"
CREATE OR REPLACE FUNCTION fn_domain_goal_delete(p_id UUID)
RETURNS VOID AS $$
BEGIN
    DELETE FROM domain_goals WHERE id = p_id;
END;
$$ LANGUAGE plpgsql;
");
    }

    public override void Down()
    {
        // Drop all functions
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_add;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_get_by_id;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_get_all;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_get_by_organization;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_get_hierarchy;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_update;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_delete;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_exists;");
        
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_add;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_get_by_id;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_get_all;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_get_by_domain;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_get_by_status;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_get_with_vote_count;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_update;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_update_priority;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_delete;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_exists;");
        
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_add;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_get_by_id;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_get_all;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_get_by_status;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_find_similar;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_get_by_feature;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_update_embedding;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_update;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_delete;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_request_exists;");
        
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feedback_add;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feedback_get_by_id;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feedback_get_by_feature;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feedback_get_by_request;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feedback_get_by_sentiment;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feedback_delete;");
        
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_vote_add;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_vote_get_by_feature;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_vote_get_count;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_feature_vote_delete;");
        
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_goal_add;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_goal_get_by_domain;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_goal_update;");
        Execute.Sql("DROP FUNCTION IF EXISTS fn_domain_goal_delete;");
    }
}
