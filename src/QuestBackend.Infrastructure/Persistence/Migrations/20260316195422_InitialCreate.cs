using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DiffJson = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    PermissionsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "config_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "enigma_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptCooldownMinutes = table.Column<int>(type: "integer", nullable: false),
                    SuccessMessage = table.Column<string>(type: "text", nullable: false),
                    FailureMessage = table.Column<string>(type: "text", nullable: false),
                    SecretCombinationJson = table.Column<string>(type: "text", nullable: false),
                    ConfigJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enigma_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "global_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswerCooldownMinutes = table.Column<int>(type: "integer", nullable: false),
                    EnigmaCooldownMinutes = table.Column<int>(type: "integer", nullable: false),
                    DefaultAnswerNormalization = table.Column<string>(type: "text", nullable: false),
                    CurrentQuestDayStateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentRoutingProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentEnigmaProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    FlagsJson = table.Column<string>(type: "text", nullable: false),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "participant_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderSubject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participant_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quest_day_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreStartMessage = table.Column<string>(type: "text", nullable: false),
                    DayClosedMessage = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_day_states", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "question_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UiMetaJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "routing_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivatedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JoinSecretHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    IsDisqualified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "enigma_rotor_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnigmaProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ColorOverride = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    PositionMin = table.Column<int>(type: "integer", nullable: false),
                    PositionMax = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MetaJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enigma_rotor_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_enigma_rotor_definitions_enigma_profiles_EnigmaProfileId",
                        column: x => x.EnigmaProfileId,
                        principalTable: "enigma_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_enigma_rotor_definitions_question_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "question_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qr_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    LastRotatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_qr_codes_question_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "question_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_pools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_pools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_question_pools_question_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "question_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BodyRichText = table.Column<string>(type: "text", nullable: false),
                    FooterHint = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    UiMetaJson = table.Column<string>(type: "text", nullable: false),
                    SupportNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    AnswerSchema = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_questions_question_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "question_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "enigma_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnigmaProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InputJson = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    CooldownAppliedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EvaluationSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enigma_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_enigma_attempts_enigma_profiles_EnigmaProfileId",
                        column: x => x.EnigmaProfileId,
                        principalTable: "enigma_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_enigma_attempts_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RemovedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RemovalReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_memberships_participant_users_ParticipantUserId",
                        column: x => x.ParticipantUserId,
                        principalTable: "participant_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_memberships_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "routing_profile_tag_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutingProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivePoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    RotationOffset = table.Column<int>(type: "integer", nullable: false),
                    SelectionMode = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_profile_tag_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_routing_profile_tag_states_question_pools_ActivePoolId",
                        column: x => x.ActivePoolId,
                        principalTable: "question_pools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_routing_profile_tag_states_question_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "question_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_routing_profile_tag_states_routing_profiles_RoutingProfileId",
                        column: x => x.RoutingProfileId,
                        principalTable: "routing_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qr_binding_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QrCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_binding_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_qr_binding_overrides_qr_codes_QrCodeId",
                        column: x => x.QrCodeId,
                        principalTable: "qr_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_qr_binding_overrides_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_qr_binding_overrides_routing_profiles_ScopeProfileId",
                        column: x => x.ScopeProfileId,
                        principalTable: "routing_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "qr_scan_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QrCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParticipantUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolutionResult = table.Column<int>(type: "integer", nullable: false),
                    ResolutionMetaJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_scan_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_qr_scan_events_qr_codes_QrCodeId",
                        column: x => x.QrCodeId,
                        principalTable: "qr_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_qr_scan_events_questions_ResolvedQuestionId",
                        column: x => x.ResolvedQuestionId,
                        principalTable: "questions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_qr_scan_events_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "question_pool_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_pool_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_question_pool_entries_question_pools_PoolId",
                        column: x => x.PoolId,
                        principalTable: "question_pools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_question_pool_entries_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_question_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstUnlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UnlockedByQrCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnlockedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSolved = table.Column<bool>(type: "boolean", nullable: false),
                    SolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RewardGrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAllowedAnswerAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_question_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_question_states_qr_codes_UnlockedByQrCodeId",
                        column: x => x.UnlockedByQrCodeId,
                        principalTable: "qr_codes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_team_question_states_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_question_states_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_rotor_rewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceQuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RewardType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GrantedByAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_rotor_rewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_rotor_rewards_question_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "question_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_rotor_rewards_questions_SourceQuestionId",
                        column: x => x.SourceQuestionId,
                        principalTable: "questions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_team_rotor_rewards_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_answer_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RawAnswer = table.Column<string>(type: "text", nullable: false),
                    NormalizedAnswer = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CooldownAppliedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EvaluationSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    TeamQuestionStateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_answer_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_answer_attempts_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_answer_attempts_team_question_states_TeamQuestionState~",
                        column: x => x.TeamQuestionStateId,
                        principalTable: "team_question_states",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_team_answer_attempts_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_OccurredAt",
                table: "admin_audit_logs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_Login",
                table: "admin_users",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enigma_attempts_AttemptedAt",
                table: "enigma_attempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_enigma_attempts_EnigmaProfileId",
                table: "enigma_attempts",
                column: "EnigmaProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_enigma_attempts_TeamId",
                table: "enigma_attempts",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_enigma_rotor_definitions_EnigmaProfileId_DisplayOrder",
                table: "enigma_rotor_definitions",
                columns: new[] { "EnigmaProfileId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_enigma_rotor_definitions_TagId",
                table: "enigma_rotor_definitions",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_participant_users_Provider_ProviderSubject",
                table: "participant_users",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qr_binding_overrides_QrCodeId_IsActive",
                table: "qr_binding_overrides",
                columns: new[] { "QrCodeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_qr_binding_overrides_QuestionId",
                table: "qr_binding_overrides",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_qr_binding_overrides_ScopeProfileId",
                table: "qr_binding_overrides",
                column: "ScopeProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_qr_codes_Slug",
                table: "qr_codes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qr_codes_TagId",
                table: "qr_codes",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_qr_scan_events_OccurredAt",
                table: "qr_scan_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_qr_scan_events_QrCodeId",
                table: "qr_scan_events",
                column: "QrCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_qr_scan_events_ResolvedQuestionId",
                table: "qr_scan_events",
                column: "ResolvedQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_qr_scan_events_TeamId",
                table: "qr_scan_events",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_quest_day_states_DayCode",
                table: "quest_day_states",
                column: "DayCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_pool_entries_PoolId_Position",
                table: "question_pool_entries",
                columns: new[] { "PoolId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_pool_entries_PoolId_QuestionId",
                table: "question_pool_entries",
                columns: new[] { "PoolId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_question_pool_entries_QuestionId",
                table: "question_pool_entries",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_question_pools_TagId",
                table: "question_pools",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_question_tags_Code",
                table: "question_tags",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_questions_TagId_Status",
                table: "questions",
                columns: new[] { "TagId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_routing_profile_tag_states_ActivePoolId",
                table: "routing_profile_tag_states",
                column: "ActivePoolId");

            migrationBuilder.CreateIndex(
                name: "IX_routing_profile_tag_states_RoutingProfileId_TagId",
                table: "routing_profile_tag_states",
                columns: new[] { "RoutingProfileId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_routing_profile_tag_states_TagId",
                table: "routing_profile_tag_states",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_team_answer_attempts_AttemptedAt",
                table: "team_answer_attempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_team_answer_attempts_QuestionId",
                table: "team_answer_attempts",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_team_answer_attempts_TeamId",
                table: "team_answer_attempts",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_team_answer_attempts_TeamQuestionStateId",
                table: "team_answer_attempts",
                column: "TeamQuestionStateId");

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_ParticipantUserId",
                table: "team_memberships",
                column: "ParticipantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_TeamId_ParticipantUserId_Status",
                table: "team_memberships",
                columns: new[] { "TeamId", "ParticipantUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_team_question_states_QuestionId",
                table: "team_question_states",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_team_question_states_TeamId_QuestionId",
                table: "team_question_states",
                columns: new[] { "TeamId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_question_states_UnlockedByQrCodeId",
                table: "team_question_states",
                column: "UnlockedByQrCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_team_rotor_rewards_SourceQuestionId",
                table: "team_rotor_rewards",
                column: "SourceQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_team_rotor_rewards_TagId",
                table: "team_rotor_rewards",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_team_rotor_rewards_TeamId_SourceQuestionId",
                table: "team_rotor_rewards",
                columns: new[] { "TeamId", "SourceQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_rotor_rewards_TeamId_TagId",
                table: "team_rotor_rewards",
                columns: new[] { "TeamId", "TagId" });

            migrationBuilder.CreateIndex(
                name: "IX_teams_Name",
                table: "teams",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_logs");

            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "config_snapshots");

            migrationBuilder.DropTable(
                name: "enigma_attempts");

            migrationBuilder.DropTable(
                name: "enigma_rotor_definitions");

            migrationBuilder.DropTable(
                name: "global_settings");

            migrationBuilder.DropTable(
                name: "qr_binding_overrides");

            migrationBuilder.DropTable(
                name: "qr_scan_events");

            migrationBuilder.DropTable(
                name: "quest_day_states");

            migrationBuilder.DropTable(
                name: "question_pool_entries");

            migrationBuilder.DropTable(
                name: "routing_profile_tag_states");

            migrationBuilder.DropTable(
                name: "team_answer_attempts");

            migrationBuilder.DropTable(
                name: "team_memberships");

            migrationBuilder.DropTable(
                name: "team_rotor_rewards");

            migrationBuilder.DropTable(
                name: "enigma_profiles");

            migrationBuilder.DropTable(
                name: "question_pools");

            migrationBuilder.DropTable(
                name: "routing_profiles");

            migrationBuilder.DropTable(
                name: "team_question_states");

            migrationBuilder.DropTable(
                name: "participant_users");

            migrationBuilder.DropTable(
                name: "qr_codes");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "question_tags");
        }
    }
}
