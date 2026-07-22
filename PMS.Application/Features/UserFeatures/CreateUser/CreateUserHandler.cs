using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Domain.Abstractions;
using PMS.Domain.Entities;

namespace PMS.Application.Features.UserFeatures.CreateUser;

/// <summary>
/// Handler for creating a new registered user.
/// Validates email uniqueness, hashes password, persists entity, and commits via UnitOfWork.
/// 
/// Request: CreateUserRequest
/// Response: Result&lt;CreateUserResponse&gt;
/// </summary>
public sealed class CreateUserHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : IHandler<CreateUserRequest, Result<CreateUserResponse>>
{
    public async Task<Result<CreateUserResponse>> HandleAsync(
        CreateUserRequest command,
        CancellationToken cancellationToken)
    {
        // 1. Check if user with the same email already exists
        bool emailExists = await dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == command.Email.ToLower(), cancellationToken);

        if (emailExists)
        {
            return UserErrors.EmailAlreadyExists(command.Email);
        }

        // 2. Create Users entity with salted BCrypt password hash
        var user = new Users
        {
            Id = Guid.NewGuid(),
            FirstName = command.FirstName.Trim(),
            MiddleName = command.MiddleName?.Trim(),
            LastName = command.LastName.Trim(),
            Email = command.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(command.Password),
            IsActive = true,
            CreatedByUserId = command.CreatedByUserId
        };

        // 3. Persist entity
        await dbContext.Users.AddAsync(user, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        // 4. Map to response (excluding PasswordHash for security)
        var response = new CreateUserResponse(
            user.Id,
            user.FirstName,
            user.MiddleName,
            user.LastName,
            user.Email,
            user.IsActive,
            user.CreatedByUserId,
            user.CreatedAt
        );

        return Result.Success(response);
    }
}
