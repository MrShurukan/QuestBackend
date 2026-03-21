using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParticipantLocalPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "participant_users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "participant_users");
        }
    }
}
