using Microsoft.Extensions.Logging;
using Otw.Clevvo.App.Members.Import.Application.Contracts;
using Otw.Clevvo.App.Members.Import.Features.MemberExtraction.Contracts;
using Otw.Clevvo.App.Members.Import.Features.DatabaseInsertion.Contracts;
using Otw.Clevvo.App.Members.Import.Features.ExcelExport.Contracts;
using Otw.Clevvo.App.Members.Import.Models;

namespace Otw.Clevvo.App.Members.Import.Application.Services;

public class ApplicationService : IApplicationService
{
    private readonly IMemberExtractionService _memberExtractionService;
    private readonly IDatabaseInsertionService _databaseInsertionService;
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<ApplicationService> _logger;
    private readonly ProcessingSummary _summary;

    public ApplicationService(
        IMemberExtractionService memberExtractionService,
        IDatabaseInsertionService databaseInsertionService,
        IExcelExportService excelExportService,
        ILogger<ApplicationService> logger)
    {
        _memberExtractionService = memberExtractionService;
        _databaseInsertionService = databaseInsertionService;
        _excelExportService = excelExportService;
        _logger = logger;
        _summary = new ProcessingSummary();
    }

    public async Task RunAsync(bool exportToExcel = true, bool insertToDatabase = false, int? startFromMemberCode = null)
    {
        _summary.StartTime = DateTime.Now;
        _summary.StartFromMemberCode = startFromMemberCode;
        _summary.DatabaseOperationPerformed = insertToDatabase;
        _summary.ExcelOperationPerformed = exportToExcel;
        
        _logger.LogInformation("🚀 A iniciar processamento de membros...");
        
        // Log the starting member code if provided via command line
        if (startFromMemberCode.HasValue)
        {
            _logger.LogInformation("📌 Código de início especificado via comando: {StartCode}", startFromMemberCode.Value);
        }
        
        try
        {
            // Step 1: Extract members from API
            var extractionResult = await _memberExtractionService.ExtractAllMembersAsync(startFromMemberCode);
            _summary.TotalMembersExtracted = extractionResult.Members.Count;
            _summary.ExtractionPages = extractionResult.PagesProcessed;
            _summary.GendersDeduced = extractionResult.GenderDeductions.Count;
            _summary.GenderDeductions = extractionResult.GenderDeductions;
            
            if (!extractionResult.Members.Any())
            {
                _logger.LogWarning("⚠️ Nenhum membro foi extraído. A terminar processamento.");
                _summary.EndTime = DateTime.Now;
                _summary.PrintReport();
                return;
            }

            var tasks = new List<Task>();

            // Step 2: Export to Excel (if requested)
            if (exportToExcel)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var excelPath = await _excelExportService.ExportToExcelAsync(extractionResult.Members);
                        _summary.ExcelExported = true;
                        _summary.ExcelFilePath = excelPath;
                        _logger.LogInformation("✅ Exportação para Excel concluída com sucesso!");
                    }
                    catch (Exception ex)
                    {
                        _summary.ExcelExported = false;
                        _logger.LogError(ex, "❌ Erro durante a exportação para Excel");
                    }
                }));
            }

            // Step 3: Insert to Database (if requested)
            if (insertToDatabase)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var result = await _databaseInsertionService.InsertMembersAsync(extractionResult.Members);
                        
                        _summary.MembersAlreadyInDatabase = result.MembersAlreadyInDatabase;
                        _summary.NewMembersInserted = result.TotalInserted;
                        _summary.MembersUpdated = result.TotalUpdated;
                        _summary.MembersNotProcessed = result.TotalNotInserted;
                        _summary.MembersWithoutChanges = result.MembersWithoutChanges;
                        _summary.FailureReasons = result.Content.MembersNotInserted
                            .Select(m => m.Reason ?? "Desconhecido")
                            .ToList();
                        
                        if (result.Success)
                        {
                            _logger.LogInformation("✅ Processamento concluído! Inseridos: {Inserted}, Atualizados: {Updated}, Total: {Total}", 
                                result.TotalInserted, result.TotalUpdated, result.TotalProcessed);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Processamento com problemas. Inseridos: {Inserted}, Atualizados: {Updated}, Falharam: {NotInserted}", 
                                result.TotalInserted, result.TotalUpdated, result.TotalNotInserted);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro durante a inserção na base de dados");
                    }
                }));
            }

            // Wait for all operations to complete
            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }

            _logger.LogInformation("🎉 Processamento concluído com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro crítico durante o processamento");
            throw;
        }
        finally
        {
            _summary.EndTime = DateTime.Now;
            _summary.PrintReport();
        }
    }
}