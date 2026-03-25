using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonalDataConsentAcceptedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PersonalDataConsentAcceptedAt",
                table: "participant_users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalDataConsentAcceptedAt",
                table: "participant_users");
        }
    }
}
