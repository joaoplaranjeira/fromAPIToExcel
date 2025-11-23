using fromAPIToExcel.Models;

namespace fromAPIToExcel.Features.ExcelExport.Contracts;

public interface IExcelExportService
{
    Task<string> ExportToExcelAsync(List<Member> members, string? outputPath = null);
}