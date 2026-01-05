using FluentMigrator;

namespace ProductIntelligence.Infrastructure.Data.Migrations
{
    [Migration(20260104004)]
    public class AddExternalIdToRoadmapItems : Migration
    {
        public override void Up()
        {
            Alter.Table("roadmap_items").AddColumn("external_id").AsString(50).Nullable();
        }

        public override void Down()
        {
            Delete.Column("external_id").FromTable("roadmap_items");
        }
    }
}
