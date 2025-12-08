using FluentMigrator;

namespace ProductIntelligence.Infrastructure.Data.Migrations;

[Migration(20251207001)]
public class InitialSchema : Migration
{
    public override void Up()
    {
        // Enable extensions
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"ltree\";");
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"vector\";");

        // Domains table (hierarchical)
        Create.Table("domains")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("organization_id").AsGuid().NotNullable()
            .WithColumn("parent_domain_id").AsGuid().Nullable().ForeignKey("domains", "id")
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("path").AsCustom("ltree").NotNullable()
            .WithColumn("owner_user_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("metadata").AsCustom("jsonb").Nullable();

        Create.Index("idx_domains_organization").OnTable("domains").OnColumn("organization_id");
        Create.Index("idx_domains_parent").OnTable("domains").OnColumn("parent_domain_id");
        Execute.Sql("CREATE INDEX idx_domains_path ON domains USING GIST (path);");

        // Features table
        Create.Table("features")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("domain_id").AsGuid().NotNullable().ForeignKey("domains", "id")
            .WithColumn("parent_feature_id").AsGuid().Nullable().ForeignKey("features", "id")
            .WithColumn("title").AsString(500).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).NotNullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Proposed")
            .WithColumn("priority").AsString(10).NotNullable().WithDefaultValue("P3")
            .WithColumn("ai_priority_score").AsDecimal(3, 2).NotNullable().WithDefaultValue(0.50)
            .WithColumn("ai_priority_reasoning").AsString(int.MaxValue).Nullable()
            .WithColumn("estimated_effort_points").AsInt32().Nullable()
            .WithColumn("business_value_score").AsDecimal(3, 2).Nullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("target_release").AsString(100).Nullable()
            .WithColumn("metadata").AsCustom("jsonb").Nullable();

        Create.Index("idx_features_domain").OnTable("features").OnColumn("domain_id");
        Create.Index("idx_features_status").OnTable("features").OnColumn("status");
        Create.Index("idx_features_priority").OnTable("features").OnColumn("priority");

        // Feature requests table
        Create.Table("feature_requests")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("title").AsString(500).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).NotNullable()
            .WithColumn("source").AsString(50).NotNullable().WithDefaultValue("Manual")
            .WithColumn("source_id").AsString(200).Nullable()
            .WithColumn("requester_name").AsString(200).NotNullable()
            .WithColumn("requester_email").AsString(200).Nullable()
            .WithColumn("requester_company").AsString(200).Nullable()
            .WithColumn("requester_tier").AsString(50).NotNullable().WithDefaultValue("Starter")
            .WithColumn("submitted_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Pending")
            .WithColumn("embedding_vector").AsCustom("vector(1536)").Nullable()
            .WithColumn("processed_at").AsDateTime().Nullable()
            .WithColumn("linked_feature_id").AsGuid().Nullable().ForeignKey("features", "id")
            .WithColumn("duplicate_of_request_id").AsGuid().Nullable().ForeignKey("feature_requests", "id")
            .WithColumn("similarity_score").AsDecimal(3, 2).Nullable()
            .WithColumn("metadata").AsCustom("jsonb").Nullable();

        Create.Index("idx_feature_requests_status").OnTable("feature_requests").OnColumn("status");
        Create.Index("idx_feature_requests_linked_feature").OnTable("feature_requests").OnColumn("linked_feature_id");
        Execute.Sql("CREATE INDEX idx_feature_requests_embedding ON feature_requests USING ivfflat (embedding_vector vector_cosine_ops) WITH (lists = 100);");

        // Feature votes table
        Create.Table("feature_votes")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("feature_id").AsGuid().Nullable().ForeignKey("features", "id")
            .WithColumn("feature_request_id").AsGuid().Nullable().ForeignKey("feature_requests", "id")
            .WithColumn("voter_email").AsString(200).NotNullable()
            .WithColumn("voter_company").AsString(200).Nullable()
            .WithColumn("voter_tier").AsString(50).NotNullable()
            .WithColumn("vote_weight").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("voted_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("idx_feature_votes_feature").OnTable("feature_votes").OnColumn("feature_id");
        Create.Index("idx_feature_votes_request").OnTable("feature_votes").OnColumn("feature_request_id");
        Execute.Sql("CREATE UNIQUE INDEX idx_feature_votes_unique_feature ON feature_votes (feature_id, voter_email) WHERE feature_id IS NOT NULL;");
        Execute.Sql("CREATE UNIQUE INDEX idx_feature_votes_unique_request ON feature_votes (feature_request_id, voter_email) WHERE feature_request_id IS NOT NULL;");

        // Feedback table
        Create.Table("feedback")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("feature_id").AsGuid().Nullable().ForeignKey("features", "id")
            .WithColumn("feature_request_id").AsGuid().Nullable().ForeignKey("feature_requests", "id")
            .WithColumn("content").AsString(int.MaxValue).NotNullable()
            .WithColumn("sentiment").AsString(20).NotNullable().WithDefaultValue("Neutral")
            .WithColumn("source").AsString(50).NotNullable()
            .WithColumn("customer_id").AsString(200).Nullable()
            .WithColumn("customer_tier").AsString(50).Nullable()
            .WithColumn("submitted_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("embedding_vector").AsCustom("vector(1536)").Nullable();

        Create.Index("idx_feedback_feature").OnTable("feedback").OnColumn("feature_id");
        Create.Index("idx_feedback_request").OnTable("feedback").OnColumn("feature_request_id");

        // Domain goals table
        Create.Table("domain_goals")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefaultValue(SystemMethods.NewGuid)
            .WithColumn("domain_id").AsGuid().NotNullable().ForeignKey("domains", "id")
            .WithColumn("goal_description").AsString(int.MaxValue).NotNullable()
            .WithColumn("target_quarter").AsString(10).Nullable()
            .WithColumn("priority").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("idx_domain_goals_domain").OnTable("domain_goals").OnColumn("domain_id");
    }

    public override void Down()
    {
        Delete.Table("domain_goals");
        Delete.Table("feedback");
        Delete.Table("feature_votes");
        Delete.Table("feature_requests");
        Delete.Table("features");
        Delete.Table("domains");
    }
}
