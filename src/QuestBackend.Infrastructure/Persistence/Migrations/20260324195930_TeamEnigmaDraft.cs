using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TeamEnigmaDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "team_enigma_drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnigmaProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_enigma_drafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_enigma_drafts_enigma_profiles_EnigmaProfileId",
                        column: x => x.EnigmaProfileId,
                        principalTable: "enigma_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_enigma_drafts_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_enigma_drafts_EnigmaProfileId",
                table: "team_enigma_drafts",
                column: "EnigmaProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_team_enigma_drafts_TeamId_EnigmaProfileId",
                table: "team_enigma_drafts",
                columns: new[] { "TeamId", "EnigmaProfileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_enigma_drafts");
        }
    }
}
