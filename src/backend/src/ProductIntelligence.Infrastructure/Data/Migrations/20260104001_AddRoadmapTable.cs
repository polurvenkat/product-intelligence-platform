using FluentMigrator;

namespace ProductIntelligence.Infrastructure.Data.Migrations;

[Migration(20260104001)]
public class AddRoadmapTable : Migration
{
    public override void Up()
    {
        Create.Table("roadmap_items")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("title").AsString(255).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("quarter").AsString(2).NotNullable() // Q1, Q2, Q3, Q4
            .WithColumn("year").AsInt32().NotNullable()
            .WithColumn("category").AsString(50).NotNullable() // Engineering, Product, Season, etc.
            .WithColumn("status").AsString(20).NotNullable() // Current, Next, Future
            .WithColumn("type").AsString(50).NotNullable() // Feature, Infrastructure, AI, etc.
            .WithColumn("progress").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("color").AsString(20).Nullable()
            .WithColumn("target_date").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("idx_roadmap_year_quarter").OnTable("roadmap_items").OnColumn("year").Ascending().OnColumn("quarter").Ascending();
        Create.Index("idx_roadmap_category").OnTable("roadmap_items").OnColumn("category");
    }

    public override void Down()
    {
        Delete.Table("roadmap_items");
    }
}
