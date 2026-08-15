using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Projects.ExportProjectExcel;

public sealed record ExportProjectExcelQuery(Guid ProjectId) : IQuery<ExportProjectExcelResponse>;
