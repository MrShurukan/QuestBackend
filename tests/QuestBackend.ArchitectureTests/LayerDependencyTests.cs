using FluentAssertions;
using NetArchTest.Rules;

namespace QuestBackend.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotDependOnApplicationInfrastructureOrApi()
    {
        var result = Types.InAssembly(typeof(QuestBackend.Domain.Shared.IClock).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("QuestBackend.Application", "QuestBackend.Infrastructure", "QuestBackend.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOnApi()
    {
        var result = Types.InAssembly(typeof(QuestBackend.Application.Abstractions.IQuestDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn("QuestBackend.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Contracts_ShouldNotDependOnDomainApplicationInfrastructureOrApi()
    {
        var result = Types.InAssembly(typeof(QuestBackend.Contracts.AdminLoginRequest).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("QuestBackend.Domain", "QuestBackend.Application", "QuestBackend.Infrastructure", "QuestBackend.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
