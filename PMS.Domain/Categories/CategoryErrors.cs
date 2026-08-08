using PMS.SharedKernel;

namespace PMS.Domain.Categories;

public static class CategoryErrors
{
    public static Error NotFound(Guid categoryId) => Error.NotFound(
        "Categories.NotFound",
        $"The category with Id '{categoryId}' was not found.");

    public static readonly Error Forbidden = Error.Failure(
        "Categories.Forbidden",
        "You do not have permission to manage this category.");

    public static readonly Error NotProjectMember = Error.Failure(
        "Categories.NotProjectMember",
        "You must be a member of the project to access categories.");

    public static readonly Error ReadOnlyAccess = Error.Failure(
        "Categories.ReadOnlyAccess",
        "Viewers do not have permission to modify categories.");
}
