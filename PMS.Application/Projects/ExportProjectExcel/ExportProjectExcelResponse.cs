namespace PMS.Application.Projects.ExportProjectExcel;

public sealed class ExportProjectExcelResponse
{
    public required byte[] FileContent { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}
