using FluentMigrator;

namespace ProductIntelligence.Infrastructure.Data.Migrations
{
    [Migration(20260104003)]
    public class AddSortOrderToRoadmaps : Migration
    {
        public override void Up()
        {
            Alter.Table("roadmap_items").AddColumn("sort_order").AsInt32().WithDefaultValue(0);
        }

        public override void Down()
        {
            Delete.Column("sort_order").FromTable("roadmap_items");
        }
    }
}
