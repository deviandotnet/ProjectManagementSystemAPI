using PMS.SharedKernel;

namespace PMS.Domain.SubCategories;

public static class SubCategoryErrors
{
    public static Error NotFound(Guid subCategoryId) => Error.NotFound(
        "SubCategories.NotFound",
        $"The subcategory with Id '{subCategoryId}' was not found.");

    public static readonly Error Forbidden = Error.Failure(
        "SubCategories.Forbidden",
        "You do not have permission to manage this subcategory.");

    public static readonly Error NotProjectMember = Error.Failure(
        "SubCategories.NotProjectMember",
        "You must be a member of the project to access subcategories.");

    public static readonly Error ReadOnlyAccess = Error.Failure(
        "SubCategories.ReadOnlyAccess",
        "Viewers do not have permission to modify subcategories.");
}
