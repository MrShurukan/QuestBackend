using FluentAssertions;
using QuestBackend.Application.Routing;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Tags;
using QuestBackend.UnitTests.Support;

namespace QuestBackend.UnitTests.Routing;

public sealed class QuestionRoutingResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldUsePoolSlotAndRotationOffset()
    {
        await using var dbContext = TestDbContextFactory.Create();

        QuestionTag tag = new() { Code = "blue", Name = "Blue", Color = "#2563EB", IsActive = true };
        Question firstQuestion = new() { Tag = tag, Title = "Q1", Status = QuestionStatus.Active, IsActive = true };
        Question secondQuestion = new() { Tag = tag, Title = "Q2", Status = QuestionStatus.Active, IsActive = true };
        QuestionPool pool = new()
        {
            Tag = tag,
            Name = "Pool",
            Entries =
            [
                new QuestionPoolEntry { Question = firstQuestion, Position = 0, IsEnabled = true },
                new QuestionPoolEntry { Question = secondQuestion, Position = 1, IsEnabled = true },
            ],
        };

        QrCode qrCode = new() { Tag = tag, Label = "QR-1", Slug = "blue0001", SlotIndex = 0, IsActive = true };
        RoutingProfile profile = new() { Name = "Default", IsActive = true };
        RoutingProfileTagState state = new()
        {
            RoutingProfile = profile,
            Tag = tag,
            ActivePool = pool,
            RotationOffset = 1,
            IsEnabled = true,
        };
        GlobalSettings settings = new() { CurrentRoutingProfileId = profile.Id };

        await dbContext.AddRangeAsync(tag, firstQuestion, secondQuestion, pool, qrCode, profile, state, settings);
        await dbContext.SaveChangesAsync();

        QuestionRoutingResolver resolver = new(dbContext);

        var resolution = await resolver.ResolveAsync(qrCode.Id);

        resolution.Question.Should().NotBeNull();
        resolution.Question!.Title.Should().Be("Q2");
    }

    [Fact]
    public async Task ResolveAsync_ShouldPreferActiveOverride()
    {
        await using var dbContext = TestDbContextFactory.Create();

        QuestionTag tag = new() { Code = "red", Name = "Red", Color = "#DC2626", IsActive = true };
        Question poolQuestion = new() { Tag = tag, Title = "PoolQuestion", Status = QuestionStatus.Active, IsActive = true };
        Question overrideQuestion = new() { Tag = tag, Title = "OverrideQuestion", Status = QuestionStatus.Active, IsActive = true };
        QuestionPool pool = new()
        {
            Tag = tag,
            Name = "Pool",
            Entries = [new QuestionPoolEntry { Question = poolQuestion, Position = 0, IsEnabled = true }],
        };
        QrCode qrCode = new() { Tag = tag, Label = "QR-1", Slug = "red00001", SlotIndex = 0, IsActive = true };
        RoutingProfile profile = new() { Name = "Default", IsActive = true };
        RoutingProfileTagState state = new()
        {
            RoutingProfile = profile,
            Tag = tag,
            ActivePool = pool,
            RotationOffset = 0,
            IsEnabled = true,
        };
        QrBindingOverride bindingOverride = new()
        {
            QrCode = qrCode,
            Question = overrideQuestion,
            ScopeProfile = profile,
            IsActive = true,
        };
        GlobalSettings settings = new() { CurrentRoutingProfileId = profile.Id };

        await dbContext.AddRangeAsync(tag, poolQuestion, overrideQuestion, pool, qrCode, profile, state, bindingOverride, settings);
        await dbContext.SaveChangesAsync();

        QuestionRoutingResolver resolver = new(dbContext);

        var resolution = await resolver.ResolveAsync(qrCode.Id);

        resolution.Question.Should().NotBeNull();
        resolution.Question!.Title.Should().Be("OverrideQuestion");
    }
}
