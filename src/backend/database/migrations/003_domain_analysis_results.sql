-- Migration: 003_domain_analysis_results
-- Description: Table to store AI document analysis results

CREATE TABLE domain_analysis_results (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    file_name VARCHAR(255) NOT NULL,
    summary TEXT NOT NULL,
    raw_json_result JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_domain_analysis_results_created_at ON domain_analysis_results(created_at DESC);
