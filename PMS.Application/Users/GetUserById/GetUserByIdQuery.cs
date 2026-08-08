using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IQuery<UserResponse>;
