using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Projects.DeleteProject;

public sealed record DeleteProjectCommand(Guid Id) : ICommand;
