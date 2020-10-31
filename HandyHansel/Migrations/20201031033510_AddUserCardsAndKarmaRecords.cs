using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace HandyHansel.Migrations
{
    public partial class AddUserCardsAndKarmaRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "all_guild_background_jobs",
                columns: table => new
                {
                    hangfire_job_id = table.Column<string>(type: "text", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: true),
                    scheduled_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    job_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_all_guild_background_jobs", x => x.hangfire_job_id);
                });

            migrationBuilder.CreateTable(
                name: "all_user_cards",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    user_timezone_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_all_user_cards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "all_user_guild_karma_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    current_karma_amount = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_all_user_guild_karma_records", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "all_guild_background_jobs");

            migrationBuilder.DropTable(
                name: "all_user_cards");

            migrationBuilder.DropTable(
                name: "all_user_guild_karma_records");
        }
    }
}
