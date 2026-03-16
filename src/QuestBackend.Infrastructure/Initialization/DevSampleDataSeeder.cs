using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestBackend.Application.Shared;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Tags;
using QuestBackend.Domain.Shared;
using QuestBackend.Infrastructure.Persistence;

namespace QuestBackend.Infrastructure.Initialization;

public sealed class DevSampleDataSeeder
{
    private readonly IClock _clock;
    private readonly IConfiguration _configuration;
    private readonly QuestDbContext _dbContext;

    public DevSampleDataSeeder(QuestDbContext dbContext, IConfiguration configuration, IClock clock)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _clock = clock;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        bool enabled = _configuration.GetValue("Bootstrap:SeedSampleData", false);
        if (!enabled)
        {
            return;
        }

        bool hasQuestions = await _dbContext.Questions.AnyAsync(cancellationToken);
        if (hasQuestions)
        {
            return;
        }

        QuestionTag blueTag = new()
        {
            Code = "blue-zone",
            Name = "Blue Zone",
            Color = "#2563EB",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        QuestionTag redTag = new()
        {
            Code = "red-zone",
            Name = "Red Zone",
            Color = "#DC2626",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        Question blueQuestion = new()
        {
            Tag = blueTag,
            Title = "Blue sample question",
            BodyRichText = "What is 2 + 2?",
            FooterHint = "Blue rotor: 4",
            Status = QuestionStatus.Active,
            IsActive = true,
            AnswerSchema = new AnswerSchema
            {
                Kind = AnswerValidationKind.Numeric,
                ExpectedNumericValue = 4,
                NumericTolerance = 0,
            },
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        Question redQuestion = new()
        {
            Tag = redTag,
            Title = "Red sample question",
            BodyRichText = "Type the word ENIGMA",
            FooterHint = "Red rotor: 7",
            Status = QuestionStatus.Active,
            IsActive = true,
            AnswerSchema = new AnswerSchema
            {
                Kind = AnswerValidationKind.NormalizedText,
                AcceptedAnswers = ["ENIGMA"],
            },
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        QuestionPool bluePool = new()
        {
            Tag = blueTag,
            Name = "Blue Default Pool",
            IsActive = true,
            SortOrder = 1,
            Entries =
            [
                new QuestionPoolEntry
                {
                    Question = blueQuestion,
                    Position = 0,
                    IsEnabled = true,
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                },
            ],
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        QuestionPool redPool = new()
        {
            Tag = redTag,
            Name = "Red Default Pool",
            IsActive = true,
            SortOrder = 1,
            Entries =
            [
                new QuestionPoolEntry
                {
                    Question = redQuestion,
                    Position = 0,
                    IsEnabled = true,
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                },
            ],
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        QrCode blueQr = new()
        {
            Tag = blueTag,
            Slug = "blue0001",
            Label = "Blue QR 1",
            SlotIndex = 0,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        QrCode redQr = new()
        {
            Tag = redTag,
            Slug = "red00001",
            Label = "Red QR 1",
            SlotIndex = 0,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        RoutingProfile routingProfile = new()
        {
            Name = "Default Routing",
            IsActive = true,
            ActivatedAt = _clock.UtcNow,
            TagStates =
            [
                new RoutingProfileTagState
                {
                    Tag = blueTag,
                    ActivePool = bluePool,
                    IsEnabled = true,
                    RotationOffset = 0,
                    SelectionMode = QuestionSelectionMode.PoolSlotRotation,
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                },
                new RoutingProfileTagState
                {
                    Tag = redTag,
                    ActivePool = redPool,
                    IsEnabled = true,
                    RotationOffset = 0,
                    SelectionMode = QuestionSelectionMode.PoolSlotRotation,
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                },
            ],
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        EnigmaProfile enigmaProfile = new()
        {
            Name = "Default Enigma",
            Mode = EnigmaMode.SimpleCombination,
            IsActive = true,
            AttemptCooldownMinutes = 5,
            SuccessMessage = "Success",
            FailureMessage = "Failure",
            SecretCombinationJson = AppJson.Serialize(
                new Dictionary<Guid, int>
                {
                    [blueTag.Id] = 4,
                    [redTag.Id] = 7,
                }),
            RotorDefinitions =
            [
                new EnigmaRotorDefinition
                {
                    Tag = blueTag,
                    Label = "Blue Rotor",
                    DisplayOrder = 1,
                    PositionMin = 1,
                    PositionMax = 9,
                    IsActive = true,
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                },
                new EnigmaRotorDefinition
                {
                    Tag = redTag,
                    Label = "Red Rotor",
                    DisplayOrder = 2,
                    PositionMin = 1,
                    PositionMax = 9,
                    IsActive = true,
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                },
            ],
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.QuestionTags.AddRangeAsync([blueTag, redTag], cancellationToken);
        await _dbContext.Questions.AddRangeAsync([blueQuestion, redQuestion], cancellationToken);
        await _dbContext.QuestionPools.AddRangeAsync([bluePool, redPool], cancellationToken);
        await _dbContext.QrCodes.AddRangeAsync([blueQr, redQr], cancellationToken);
        await _dbContext.RoutingProfiles.AddAsync(routingProfile, cancellationToken);
        await _dbContext.EnigmaProfiles.AddAsync(enigmaProfile, cancellationToken);

        GlobalSettings? settings = await _dbContext.GlobalSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            settings.CurrentRoutingProfileId = routingProfile.Id;
            settings.CurrentEnigmaProfileId = enigmaProfile.Id;
            settings.UpdatedAt = _clock.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
