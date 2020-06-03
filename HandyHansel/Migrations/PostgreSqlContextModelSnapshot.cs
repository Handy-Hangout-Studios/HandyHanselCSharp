﻿// <auto-generated />
using HandyHansel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HandyHansel.Migrations
{
    [DbContext(typeof(PostgreSqlContext))]
    partial class PostgreSqlContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "5.0.0-preview.4.20220.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("HandyHansel.Models.GuildTimeZone", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Guild")
                        .HasColumnName("guild")
                        .HasColumnType("text");

                    b.Property<string>("OperatingSystem")
                        .HasColumnName("operating_system")
                        .HasColumnType("text");

                    b.Property<string>("TimeZoneId")
                        .HasColumnName("timezone_id")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("all_guild_time_zones");
                });
#pragma warning restore 612, 618
        }
    }
}
