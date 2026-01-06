using FluentMigrator;

namespace ProductIntelligence.Infrastructure.Data.Migrations;

[Migration(20260105001)]
public class AddSentimentConfidenceToFeedback : Migration
{
    public override void Up()
    {
        Alter.Table("feedback")
            .AddColumn("sentiment_confidence").AsDecimal(5, 4).NotNullable().WithDefaultValue(0.0);
    }

    public override void Down()
    {
        Delete.Column("sentiment_confidence").FromTable("feedback");
    }
}
