using FluentMigrator;

namespace ProductIntelligence.Infrastructure.Data.Migrations;

[Migration(20260104002)]
public class AddPriorityToRoadmapItems : Migration
{
    public override void Up()
    {
        Alter.Table("roadmap_items")
            .AddColumn("priority").AsInt32().NotNullable().WithDefaultValue(1);
    }

    public override void Down()
    {
        Delete.Column("priority").FromTable("roadmap_items");
    }
}
