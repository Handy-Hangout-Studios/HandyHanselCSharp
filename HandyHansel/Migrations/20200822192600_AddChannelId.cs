using Microsoft.EntityFrameworkCore.Migrations;

namespace HandyHansel.Migrations
{
    public partial class AddChannelId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "channel_id",
                table: "all_guild_scheduled_events",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "channel_id",
                table: "all_guild_scheduled_events");
        }
    }
}
