using FluentMigrator;

namespace ProductIntelligence.Infrastructure.Data.Migrations;

[Migration(20251225002)]
public class AddUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(new RawSql("gen_random_uuid()"))
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("name").AsString(255).NotNullable()
            .WithColumn("company").AsString(255).Nullable()
            .WithColumn("password_hash").AsString(500).NotNullable()
            .WithColumn("tier").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("last_login_at").AsDateTime().Nullable()
            .WithColumn("refresh_token").AsString(500).Nullable()
            .WithColumn("refresh_token_expires_at").AsDateTime().Nullable();
        
        Create.Index("idx_users_email")
            .OnTable("users")
            .OnColumn("email")
            .Ascending();
        
        Create.Index("idx_users_refresh_token")
            .OnTable("users")
            .OnColumn("refresh_token")
            .Ascending();
    }

    public override void Down()
    {
        Delete.Table("users");
    }
}
