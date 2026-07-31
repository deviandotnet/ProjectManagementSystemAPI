using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Users.CreateUser;

internal sealed class CreateUserCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = command.Email.Trim().ToLowerInvariant();

        bool emailExists = await context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Result.Failure<Guid>(UserErrors.EmailAlreadyExists(command.Email));
        }

        string passwordHash = passwordHasher.Hash(command.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = command.FirstName.Trim(),
            MiddleName = command.MiddleName?.Trim(),
            LastName = command.LastName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            IsActive = true
        };

        user.Raise(new UserCreatedDomainEvent(user.Id));

        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return user.Id;
    }
}
