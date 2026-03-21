using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaxTeamMembersGlobalSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTeamMembers",
                table: "global_settings",
                type: "integer",
                nullable: false,
                defaultValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTeamMembers",
                table: "global_settings");
        }
    }
}
