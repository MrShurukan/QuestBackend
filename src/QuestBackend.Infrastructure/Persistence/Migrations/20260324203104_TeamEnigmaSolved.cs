using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TeamEnigmaSolved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnigmaSolvedAt",
                table: "teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EnigmaSolvedProfileId",
                table: "teams",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnigmaSolvedAt",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "EnigmaSolvedProfileId",
                table: "teams");
        }
    }
}
