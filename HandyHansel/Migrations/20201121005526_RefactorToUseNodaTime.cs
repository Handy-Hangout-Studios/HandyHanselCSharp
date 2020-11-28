using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using System;

namespace HandyHansel.Migrations
{
    public partial class RefactorToUseNodaTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "operating_system",
                table: "all_user_time_zones");

            migrationBuilder.AddColumn<Instant>(
                name: "timestamp",
                table: "all_guild_moderation_audit_records",
                type: "timestamp",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AlterColumn<Instant>(
                name: "scheduled_time",
                table: "all_guild_background_jobs",
                type: "timestamp",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "timestamp",
                table: "all_guild_moderation_audit_records");

            migrationBuilder.AddColumn<string>(
                name: "operating_system",
                table: "all_user_time_zones",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "scheduled_time",
                table: "all_guild_background_jobs",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(Instant),
                oldType: "timestamp");
        }
    }
}
