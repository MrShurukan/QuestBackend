using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TeamFinalTaskPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalTaskPhotoUploadedAt",
                table: "teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalTaskPhotoUrl",
                table: "teams",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalTaskPhotoUploadedAt",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "FinalTaskPhotoUrl",
                table: "teams");
        }
    }
}
