﻿// <auto-generated />
using HandyHansel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HandyHansel.Migrations
{
    [DbContext(typeof(PostgreSqlContext))]
    [Migration("20201121005526_RefactorToUseNodaTime")]
    partial class RefactorToUseNodaTime
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("HandyHansel.BotDatabase.GuildKarmaRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("CurrentKarma")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("current_karma_amount");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.ToTable("all_user_guild_karma_records");
                });

            modelBuilder.Entity("HandyHansel.BotDatabase.Models.GuildLogsChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("log_channel_id");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.HasKey("Id");

                    b.ToTable("all_guild_log_channels");
                });

            modelBuilder.Entity("HandyHansel.BotDatabase.Models.GuildModerationAuditRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("ModerationAction")
                        .HasColumnType("integer")
                        .HasColumnName("moderation_action_type");

                    b.Property<decimal>("ModeratorUserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("moderator_user_id");

                    b.Property<string>("Reason")
                        .HasColumnType("text")
                        .HasColumnName("reason");

                    b.Property<Instant>("Timestamp")
                        .HasColumnType("timestamp")
                        .HasColumnName("timestamp");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.ToTable("all_guild_moderation_audit_records");
                });

            modelBuilder.Entity("HandyHansel.BotDatabase.UserCard", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.Property<int>("UserTimeZoneId")
                        .HasColumnType("integer")
                        .HasColumnName("user_timezone_id");

                    b.HasKey("Id");

                    b.ToTable("all_user_cards");
                });

            modelBuilder.Entity("HandyHansel.Models.GuildBackgroundJob", b =>
                {
                    b.Property<string>("HangfireJobId")
                        .HasColumnType("text")
                        .HasColumnName("hangfire_job_id");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<int>("GuildJobType")
                        .HasColumnType("integer")
                        .HasColumnName("job_type");

                    b.Property<string>("JobName")
                        .HasColumnType("text")
                        .HasColumnName("job_name");

                    b.Property<Instant>("ScheduledTime")
                        .HasColumnType("timestamp")
                        .HasColumnName("scheduled_time");

                    b.HasKey("HangfireJobId");

                    b.ToTable("all_guild_background_jobs");
                });

            modelBuilder.Entity("HandyHansel.Models.GuildEvent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("EventDesc")
                        .HasColumnType("text")
                        .HasColumnName("event_description");

                    b.Property<string>("EventName")
                        .HasColumnType("text")
                        .HasColumnName("event_name");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.HasKey("Id");

                    b.ToTable("all_guild_events");
                });

            modelBuilder.Entity("HandyHansel.Models.GuildPrefix", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<string>("Prefix")
                        .HasColumnType("text")
                        .HasColumnName("prefix");

                    b.HasKey("Id");

                    b.ToTable("all_guild_prefixes");
                });

            modelBuilder.Entity("HandyHansel.Models.UserTimeZone", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("TimeZoneId")
                        .HasColumnType("text")
                        .HasColumnName("timezone_id");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.ToTable("all_user_time_zones");
                });
#pragma warning restore 612, 618
        }
    }
}
