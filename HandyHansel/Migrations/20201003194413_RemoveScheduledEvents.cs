using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HandyHansel.Migrations
{
    public partial class RemoveScheduledEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "all_guild_scheduled_events");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "all_guild_scheduled_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    announced = table.Column<bool>(type: "boolean", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild_event_id = table.Column<int>(type: "integer", nullable: false),
                    scheduled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_all_guild_scheduled_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_all_guild_scheduled_events_all_guild_events_guild_event_id",
                        column: x => x.guild_event_id,
                        principalTable: "all_guild_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_all_guild_scheduled_events_guild_event_id",
                table: "all_guild_scheduled_events",
                column: "guild_event_id");
        }
    }
}
