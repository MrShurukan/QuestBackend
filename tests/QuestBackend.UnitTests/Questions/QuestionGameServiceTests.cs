using FluentAssertions;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Questions;
using QuestBackend.Application.Teams;
using QuestBackend.Contracts;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Participants;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Tags;
using QuestBackend.Domain.Teams;
using QuestBackend.UnitTests.Support;

namespace QuestBackend.UnitTests.Questions;

public sealed class QuestionGameServiceTests
{
    [Fact]
    public async Task SubmitAnswerAsync_ShouldApplyCooldownAndBlockRepeatedWrongAttempts()
    {
        await using var dbContext = TestDbContextFactory.Create();
        FakeClock clock = new();
        FakeCurrentPrincipal principal = new()
        {
            ParticipantUserId = Guid.NewGuid(),
            IsParticipantAuthenticated = true,
        };

        ParticipantUser participant = new()
        {
            Id = principal.ParticipantUserId.Value,
            Provider = "dev",
            ProviderSubject = "participant-1",
            DisplayName = "Tester",
        };

        Team team = new() { Name = "Team", JoinSecretHash = "HASH::secret" };
        TeamMembership membership = new()
        {
            Team = team,
            ParticipantUser = participant,
            ParticipantUserId = participant.Id,
            Status = TeamMembershipStatus.Active,
        };
        QuestionTag tag = new() { Code = "blue", Name = "Blue", Color = "#2563EB", IsActive = true };
        Question question = new()
        {
            Tag = tag,
            Title = "Question",
            Status = QuestionStatus.Active,
            IsActive = true,
            AnswerSchema = new AnswerSchema
            {
                Kind = AnswerValidationKind.NormalizedText,
                AcceptedAnswers = ["RIGHT"],
            },
        };
        TeamQuestionState state = new()
        {
            Team = team,
            Question = question,
            FirstUnlockedAt = clock.UtcNow,
        };
        GlobalSettings settings = new() { AnswerCooldownMinutes = 5 };

        await dbContext.AddRangeAsync(participant, team, membership, tag, question, state, settings);
        await dbContext.SaveChangesAsync();

        TeamService teamService = new(dbContext, new FakePasswordHasher(), principal, clock);
        QuestionGameService service = new(
            dbContext,
            new StaticQuestionRoutingResolver(new QuestionRoutingResolution(QrScanResolutionResult.Resolved, null, question, "resolved")),
            new AlwaysOpenLifecycleGate(),
            new AnswerEvaluator(),
            principal,
            teamService,
            clock);

        SubmitAnswerResponse wrongResponse = await service.SubmitAnswerAsync(question.Id, new SubmitAnswerRequest("wrong"));
        SubmitAnswerResponse blockedResponse = await service.SubmitAnswerAsync(question.Id, new SubmitAnswerRequest("wrong-again"));

        wrongResponse.Result.Should().Be("wrong");
        wrongResponse.NextAllowedAnswerAt.Should().NotBeNull();
        blockedResponse.Result.Should().Be("cooldown");
    }

    [Fact]
    public async Task SubmitAnswerAsync_ShouldGrantOnlyOneReward_WhenQuestionBecomesSolved()
    {
        await using var dbContext = TestDbContextFactory.Create();
        FakeClock clock = new();
        FakeCurrentPrincipal principal = new()
        {
            ParticipantUserId = Guid.NewGuid(),
            IsParticipantAuthenticated = true,
        };

        ParticipantUser participant = new()
        {
            Id = principal.ParticipantUserId.Value,
            Provider = "dev",
            ProviderSubject = "participant-1",
            DisplayName = "Tester",
        };

        Team team = new() { Name = "Team", JoinSecretHash = "HASH::secret" };
        TeamMembership membership = new()
        {
            Team = team,
            ParticipantUser = participant,
            ParticipantUserId = participant.Id,
            Status = TeamMembershipStatus.Active,
        };
        QuestionTag tag = new() { Code = "red", Name = "Red", Color = "#DC2626", IsActive = true };
        Question question = new()
        {
            Tag = tag,
            Title = "Question",
            FooterHint = "Hint",
            Status = QuestionStatus.Active,
            IsActive = true,
            AnswerSchema = new AnswerSchema
            {
                Kind = AnswerValidationKind.NormalizedText,
                AcceptedAnswers = ["RIGHT"],
            },
        };
        TeamQuestionState state = new()
        {
            Team = team,
            Question = question,
            FirstUnlockedAt = clock.UtcNow,
        };
        GlobalSettings settings = new() { AnswerCooldownMinutes = 5 };

        await dbContext.AddRangeAsync(participant, team, membership, tag, question, state, settings);
        await dbContext.SaveChangesAsync();

        TeamService teamService = new(dbContext, new FakePasswordHasher(), principal, clock);
        QuestionGameService service = new(
            dbContext,
            new StaticQuestionRoutingResolver(new QuestionRoutingResolution(QrScanResolutionResult.Resolved, null, question, "resolved")),
            new AlwaysOpenLifecycleGate(),
            new AnswerEvaluator(),
            principal,
            teamService,
            clock);

        SubmitAnswerResponse firstResponse = await service.SubmitAnswerAsync(question.Id, new SubmitAnswerRequest("RIGHT"));
        SubmitAnswerResponse secondResponse = await service.SubmitAnswerAsync(question.Id, new SubmitAnswerRequest("RIGHT"));

        firstResponse.Result.Should().Be("correct");
        firstResponse.RewardGranted.Should().BeTrue();
        secondResponse.Result.Should().Be("already_solved");
        dbContext.TeamRotorRewards.Should().HaveCount(1);
    }
}
