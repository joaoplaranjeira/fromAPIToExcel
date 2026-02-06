using Otw.Clevvo.App.Members.Import.Models;

namespace Otw.Clevvo.App.Members.Import.Features.ExcelExport.Contracts;

public interface IExcelExportService
{
    Task<string> ExportToExcelAsync(List<Member> members, string? outputPath = null);
}