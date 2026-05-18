using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NUnit.Framework;
using SS14.Labeller.Repository;

namespace SS14.Labeller.Tests.IntegrationTests.Repository;

public class DiscourseTopicsRepositoryTests
{
    private IDiscourseTopicsRepository _repository;

    [SetUp]
    public void Setup()
    {
        _repository = new DiscourseTopicsRepository(TestSetup.Configuration);

        CleanUpDb();
    }

    [Test]
    public async Task HasTopic_NoSuchParamsCombination_IsFalse()
    {
        // Arrange

        // Act
        var actual = await _repository.HasTopic("some-random-owner", "some-random-name", 54353, default);

        // Assert
        Assert.That(actual, Is.False);
    }


    [Test]
    public async Task Add_DoesNotExist_IsAdded()
    {
        // Arrange
        const string owner = "some-random-owner";
        const string repoName = "some-random-name";
        const int issueNumber = 54353;
        var before = await _repository.HasTopic(owner, repoName, issueNumber, default);

        // Act
        await _repository.Add(owner, repoName, issueNumber, 1231, default);

        // Assert
        var after = await _repository.HasTopic(owner, repoName, issueNumber, default);

        Assert.That(before, Is.False);
        Assert.That(after, Is.True);
    }

    [TearDown]
    public void TearDown()
    {
        CleanUpDb();
    }

    private static void CleanUpDb()
    {
        var connectionString = TestSetup.Configuration.GetConnectionString("Default");
        using var con = new NpgsqlConnection(connectionString);
        con.Open();
        con.Execute("TRUNCATE TABLE discourse.discussions");
    }
}
