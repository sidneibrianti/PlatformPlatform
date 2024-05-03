using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record UpdateUserCommand : ICommand, IUserValidation, IRequest<Result>
{
    [JsonIgnore] // Removes the Id from the API contract
    public UserId Id { get; init; } = null!;
    
    public required UserRole UserRole { get; init; }
    
    public required string Email { get; init; }
}

public sealed class UpdateUserValidator : UserValidator<UpdateUserCommand>;

public sealed class UpdateUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateUserCommand, Result>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");
        
        user.UpdateEmail(command.Email);
        user.ChangeUserRole(command.UserRole);
        userRepository.Update(user);
        
        events.CollectEvent(new UserUpdated());
        
        return Result.Success();
    }
}
