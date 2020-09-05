using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HandyHansel.Migrations
{
    // ReSharper disable once InconsistentNaming
    public partial class Create_Events_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "guild",
                table: "all_guild_time_zones",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "all_guild_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_all_guild_events", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "all_guild_events");

            migrationBuilder.AlterColumn<string>(
                name: "guild",
                table: "all_guild_time_zones",
                type: "text",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");
        }
    }
}
