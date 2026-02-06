using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Otw.Clevvo.App.Members.Import.Features.ExcelExport.Contracts;
using Otw.Clevvo.App.Members.Import.Infrastructure.Configuration;
using Otw.Clevvo.App.Members.Import.Models;

namespace Otw.Clevvo.App.Members.Import.Features.ExcelExport.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly ILogger<ExcelExportService> _logger;
    private readonly AppSettings _settings;

    public ExcelExportService(ILogger<ExcelExportService> logger, IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<string> ExportToExcelAsync(List<Member> members, string? outputPath = null)
    {
        _logger.LogInformation("📊 A iniciar exportação para Excel. Total de membros: {Count}", members.Count);

        var fileName = outputPath ?? _settings.Excel.OutputFileName;

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_settings.Excel.WorksheetName);

            // Headers
            worksheet.Cell(1, 1).Value = "Nr Socio | Nome";
            
            for (int i = 0; i < _settings.MemberAttributes.Count; i++)
            {
                worksheet.Cell(1, i + 3).Value = _settings.MemberAttributes[i];
            }

            // Format headers
            var headerRange = worksheet.Range(1, 1, 1, _settings.MemberAttributes.Count + 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data rows
            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                worksheet.Cell(i + 2, 1).Value = member.Title.Replace("#", "");

                for (int j = 0; j < _settings.MemberAttributes.Count; j++)
                {
                    var field = member.Fields.FirstOrDefault(f => f.Attribute == _settings.MemberAttributes[j]);
                    var value = field?.Value?.ToString() ?? "";
                    
                    // Handle different data types appropriately
                    if (DateTime.TryParse(value, out var dateValue))
                    {
                        worksheet.Cell(i + 2, j + 3).Value = dateValue;
                        worksheet.Cell(i + 2, j + 3).Style.DateFormat.Format = "dd/MM/yyyy";
                    }
                    else if (decimal.TryParse(value, out var decimalValue))
                    {
                        worksheet.Cell(i + 2, j + 3).Value = decimalValue;
                    }
                    else if (int.TryParse(value, out var intValue))
                    {
                        worksheet.Cell(i + 2, j + 3).Value = intValue;
                    }
                    else
                    {
                        worksheet.Cell(i + 2, j + 3).Value = value;
                    }
                }

                // Progress logging every 100 rows
                if ((i + 1) % 100 == 0)
                {
                    _logger.LogDebug("📝 Processadas {Processed} linhas de {Total}...", i + 1, members.Count);
                }
            }

            // Auto-fit columns
            worksheet.ColumnsUsed().AdjustToContents();

            // Add filters to headers
            var usedRange = worksheet.RangeUsed();
            if (usedRange != null)
            {
                usedRange.SetAutoFilter();
            }

            workbook.SaveAs(fileName);
        });

        var fullPath = Path.GetFullPath(fileName);
        _logger.LogInformation("✅ Excel exportado com sucesso: {FileName}", fileName);
        return fullPath;
    }
}