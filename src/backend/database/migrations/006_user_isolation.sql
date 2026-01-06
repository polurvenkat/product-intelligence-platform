-- Migration: 006_org_rag_isolation
-- Description: Adds organization_id to business_context and domain_analysis_results for org-wide RAG learning, and user_id to chat_sessions for private chat history.

ALTER TABLE business_context ADD COLUMN organization_id UUID;
ALTER TABLE domain_analysis_results ADD COLUMN organization_id UUID;

CREATE INDEX idx_business_context_org_id ON business_context(organization_id);
CREATE INDEX idx_domain_analysis_results_org_id ON domain_analysis_results(organization_id);

-- Ensure chat_sessions table exists from 005 and has user_id
-- (Migration 005 already added user_id to chat_sessions)
