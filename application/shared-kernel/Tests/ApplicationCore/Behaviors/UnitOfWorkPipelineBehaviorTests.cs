using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;
using PlatformPlatform.SharedKernel.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.ApplicationCore.Behaviors;

public sealed class UnitOfWorkPipelineBehaviorTests
{
    private readonly UnitOfWorkPipelineBehavior<TestCommand, Result<TestAggregate>> _behavior;
    private readonly IUnitOfWork _unitOfWork;
    
    public UnitOfWorkPipelineBehaviorTests()
    {
        var services = new ServiceCollection();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        services.AddSingleton(_unitOfWork);
        _behavior = new UnitOfWorkPipelineBehavior<TestCommand, Result<TestAggregate>>(
            _unitOfWork,
            new ConcurrentCommandCounter()
        );
    }
    
    [Fact]
    public async Task Handle_WhenSuccessfulCommand_ShouldCallNextAndCommitChanges()
    {
        // Arrange
        var command = new TestCommand();
        var cancellationToken = new CancellationToken();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        var successfulCommandResult = Result<TestAggregate>.Success(TestAggregate.Create("Foo"));
        next.Invoke().Returns(Task.FromResult(successfulCommandResult));
        
        // Act
        _ = await _behavior.Handle(command, next, cancellationToken);
        
        // Assert
        await _unitOfWork.Received().CommitAsync(cancellationToken);
        Received.InOrder(() =>
            {
                next.Invoke();
                _unitOfWork.CommitAsync(cancellationToken);
            }
        );
    }
    
    [Fact]
    public async Task Handle_WhenNonSuccessfulCommand_ShouldCallNextButNotCommitChanges()
    {
        // Arrange
        var command = new TestCommand();
        var cancellationToken = new CancellationToken();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        var successfulCommandResult = Result<TestAggregate>.BadRequest("Fail");
        next.Invoke().Returns(Task.FromResult(successfulCommandResult));
        
        // Act
        _ = await _behavior.Handle(command, next, cancellationToken);
        
        // Assert
        await _unitOfWork.DidNotReceive().CommitAsync(cancellationToken);
        await next.Received().Invoke();
    }
}
