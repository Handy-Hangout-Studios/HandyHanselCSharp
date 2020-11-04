using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace HandyHansel.Migrations
{
    public partial class FixAccidentalOmissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "guild_event_id",
                table: "all_guild_scheduled_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_date",
                table: "all_guild_scheduled_events",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "event_description",
                table: "all_guild_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_name",
                table: "all_guild_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "guild",
                table: "all_guild_events",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_all_guild_scheduled_events_guild_event_id",
                table: "all_guild_scheduled_events",
                column: "guild_event_id");

            migrationBuilder.AddForeignKey(
                name: "FK_all_guild_scheduled_events_all_guild_events_guild_event_id",
                table: "all_guild_scheduled_events",
                column: "guild_event_id",
                principalTable: "all_guild_events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_all_guild_scheduled_events_all_guild_events_guild_event_id",
                table: "all_guild_scheduled_events");

            migrationBuilder.DropIndex(
                name: "IX_all_guild_scheduled_events_guild_event_id",
                table: "all_guild_scheduled_events");

            migrationBuilder.DropColumn(
                name: "guild_event_id",
                table: "all_guild_scheduled_events");

            migrationBuilder.DropColumn(
                name: "scheduled_date",
                table: "all_guild_scheduled_events");

            migrationBuilder.DropColumn(
                name: "event_description",
                table: "all_guild_events");

            migrationBuilder.DropColumn(
                name: "event_name",
                table: "all_guild_events");

            migrationBuilder.DropColumn(
                name: "guild",
                table: "all_guild_events");
        }
    }
}
