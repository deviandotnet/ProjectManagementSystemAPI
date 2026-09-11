namespace PMS.Application.Abstractions.Caching;

public static class CacheTags
{
    public const string Holidays = "pms:holidays";
    public const string WorkingDays = "pms:working-days";
    public const string AllProjectLists = "pms:all-project-lists";
    public const string AllDashboards = "pms:all-dashboards";

    public static string Project(Guid projectId) => $"pms:project:{projectId:N}";
    public static string ProjectDetails(Guid projectId) => $"{Project(projectId)}:details";
    public static string ProjectMembers(Guid projectId) => $"{Project(projectId)}:members";
    public static string ProjectCategories(Guid projectId) => $"{Project(projectId)}:categories";
    public static string ProjectActionItems(Guid projectId) => $"{Project(projectId)}:action-items";
    public static string ProjectTimeline(Guid projectId) => $"{Project(projectId)}:timeline";
    public static string ProjectProgress(Guid projectId) => $"{Project(projectId)}:progress";
    public static string ProjectAudit(Guid projectId) => $"{Project(projectId)}:audit";
    public static string CategorySubCategories(Guid categoryId) => $"pms:category:{categoryId:N}:subcategories";
    public static string ActionItem(Guid actionItemId) => $"pms:action-item:{actionItemId:N}";
    public static string ActionItemHistory(Guid actionItemId) => $"{ActionItem(actionItemId)}:history";
    public static string UserProjects(Guid userId) => $"pms:user:{userId:N}:projects";
    public static string UserDashboard(Guid userId) => $"pms:user:{userId:N}:dashboard";
    public static string UserProfile(Guid userId) => $"pms:user:{userId:N}:profile";
}
