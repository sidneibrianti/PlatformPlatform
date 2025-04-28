using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.StronglyTypedIds;
using PlatformPlatform.SharedKernel.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.Persistence;

public sealed class RepositoryTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory<TestDbContext> _sqliteInMemoryDbContextFactory;
    private readonly TestAggregateRepository _testAggregateRepository;
    private readonly TestDbContext _testDbContext;

    public RepositoryTests()
    {
        var executionContext = new BackgroundWorkerExecutionContext();
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<TestDbContext>(executionContext);
        _testDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        _testAggregateRepository = new TestAggregateRepository(_testDbContext);
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
    }

    [Fact]
    public async Task Add_WhenNewAggregate_ShouldAddToDatabase()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("TestAggregate");

        // Act
        await _testAggregateRepository.AddAsync(testAggregate, CancellationToken.None);
        await _testDbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var retrievedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregate.Id, CancellationToken.None);
        retrievedAggregate.Should().NotBeNull();
        retrievedAggregate.Id.Should().Be(testAggregate.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAggregateExists_ShouldRetrieveFromDatabase()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Act
        var retrievedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregate.Id, CancellationToken.None);

        // Assert
        retrievedAggregate.Should().NotBeNull();
        retrievedAggregate.Id.Should().Be(testAggregate.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAggregateDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = IdGenerator.NewId();

        // Act
        var retrievedAggregate = await _testAggregateRepository.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        retrievedAggregate.Should().BeNull();
    }

    [Fact]
    public async Task Update_WhenExistingAggregate_ShouldUpdateDatabase()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();
        var initialName = testAggregate.Name;

        // Act
        testAggregate.Name = "UpdatedName";
        _testAggregateRepository.Update(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert
        var updatedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregate.Id, CancellationToken.None);
        updatedAggregate.Should().NotBeNull();
        updatedAggregate.Name.Should().NotBe(initialName);
        updatedAggregate.Name.Should().Be("UpdatedName");
    }

    [Fact]
    public async Task Update_WhenAlreadyTracked_ShouldUpdateDatabase()
    {
        // Arrange
        var testAggregateAdd = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(testAggregateAdd);
        var initialName = testAggregateAdd.Name;

        // Act
        var testAggregateUpdate = new TestAggregate("UpdatedName") { Id = testAggregateAdd.Id };
        _testAggregateRepository.Update(testAggregateUpdate);
        await _testDbContext.SaveChangesAsync();

        // Assert
        var updatedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregateUpdate.Id, CancellationToken.None);
        updatedAggregate.Should().NotBeNull();
        updatedAggregate.Name.Should().NotBe(initialName);
        updatedAggregate.Name.Should().Be("UpdatedName");
    }

    [Fact]
    public async Task Remove_WhenExistingAggregate_ShouldRemoveFromDatabase()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Act
        _testAggregateRepository.Remove(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert
        var retrievedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregate.Id, CancellationToken.None);
        retrievedAggregate.Should().BeNull();
    }

    [Fact]
    public async Task Update_WhenEntityIsModifiedByAnotherUser_ShouldThrowConcurrencyException()
    {
        // Arrange
        var primaryRepository = new TestAggregateRepository(_testDbContext);
        var originalTestAggregate = TestAggregate.Create("TestAggregate");
        await primaryRepository.AddAsync(originalTestAggregate, CancellationToken.None);
        await _testDbContext.SaveChangesAsync(CancellationToken.None);

        // Simulate another user by creating a new DbContext and repository instance
        var secondaryDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        var secondaryRepository = new TestAggregateRepository(secondaryDbContext);

        // Act
        var concurrentTestAggregate =
            (await secondaryRepository.GetByIdAsync(originalTestAggregate.Id, CancellationToken.None))!;
        concurrentTestAggregate.Name = "UpdatedTestAggregateByAnotherUser";
        secondaryRepository.Update(concurrentTestAggregate);
        await secondaryDbContext.SaveChangesAsync(CancellationToken.None);

        originalTestAggregate.Name = "UpdatedTestAggregate";
        primaryRepository.Update(originalTestAggregate);

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _testDbContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task EntityModification_WhenRepositoryUpdateNotCalled_ShouldNotTrackChanges()
    {
        // Arrange
        var seedingTestAggregate = TestAggregate.Create("TestAggregate");
        var seedingTestDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        seedingTestDbContext.TestAggregates.Add(seedingTestAggregate);
        await seedingTestDbContext.SaveChangesAsync();
        var testAggregateId = seedingTestAggregate.Id;

        // Act
        var testAggregate = (await _testAggregateRepository.GetByIdAsync(testAggregateId, CancellationToken.None))!;
        testAggregate.Name = "UpdatedTestAggregate";

        // Assert
        _testDbContext.ChangeTracker.Entries<TestAggregate>().Count().Should().Be(0);
        var affectedRows = await _testDbContext.SaveChangesAsync();
        affectedRows.Should().Be(0);
    }
}
