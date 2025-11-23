namespace fromAPIToExcel.Models;

public class ProcessingSummary
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    // Extraction Summary
    public int TotalMembersExtracted { get; set; }
    public int? StartFromMemberCode { get; set; }
    public int ExtractionPages { get; set; }
    
    // Database Summary
    public int MembersAlreadyInDatabase { get; set; }
    public int NewMembersInserted { get; set; }
    public int MembersUpdated { get; set; }
    public int MembersNotProcessed { get; set; }
    public int MembersWithoutChanges { get; set; }
    public List<string> FailureReasons { get; set; } = new();
    
    // Excel Summary
    public bool ExcelExported { get; set; }
    public string? ExcelFilePath { get; set; }
    
    // Operations performed
    public bool DatabaseOperationPerformed { get; set; }
    public bool ExcelOperationPerformed { get; set; }

    public void PrintReport()
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("                    📊 RELATÓRIO FINAL DO PROCESSO                ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();
        
        // Time information
        Console.WriteLine("⏱️  TEMPO DE EXECUÇÃO");
        Console.WriteLine($"   • Início: {StartTime:HH:mm:ss}");
        Console.WriteLine($"   • Fim: {EndTime:HH:mm:ss}");
        Console.WriteLine($"   • Duração: {Duration.Hours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}");
        Console.WriteLine();
        
        // Extraction summary
        Console.WriteLine("📥 EXTRAÇÃO DE DADOS");
        Console.WriteLine($"   • Total de membros extraídos: {TotalMembersExtracted}");
        if (StartFromMemberCode.HasValue)
        {
            Console.WriteLine($"   • Código de início: {StartFromMemberCode.Value}");
        }
        Console.WriteLine($"   • Páginas processadas: {ExtractionPages}");
        Console.WriteLine();
        
        // Database summary
        if (DatabaseOperationPerformed)
        {
            Console.WriteLine("🗄️  BASE DE DADOS");
            Console.WriteLine($"   • Membros já existentes: {MembersAlreadyInDatabase}");
            Console.WriteLine($"   • Novos membros inseridos: {NewMembersInserted}");
            Console.WriteLine($"   • Membros atualizados: {MembersUpdated}");
            Console.WriteLine($"   • Membros sem alterações: {MembersWithoutChanges}");
            
            if (MembersNotProcessed > 0)
            {
                Console.WriteLine($"   • Membros não processados: {MembersNotProcessed}");
                
                if (FailureReasons.Any())
                {
                    Console.WriteLine("   • Principais motivos de falha:");
                    var groupedReasons = FailureReasons.GroupBy(r => r)
                        .OrderByDescending(g => g.Count())
                        .Take(5);
                    
                    foreach (var reason in groupedReasons)
                    {
                        Console.WriteLine($"     - {reason.Key}: {reason.Count()} ocorrência(s)");
                    }
                }
            }
            
            var totalProcessed = NewMembersInserted + MembersUpdated;
            var successRate = TotalMembersExtracted > 0 
                ? (totalProcessed * 100.0 / TotalMembersExtracted) 
                : 0;
            Console.WriteLine($"   • Taxa de sucesso: {successRate:F1}%");
            Console.WriteLine();
        }
        
        // Excel summary
        if (ExcelOperationPerformed)
        {
            Console.WriteLine("📊 EXPORTAÇÃO EXCEL");
            if (ExcelExported)
            {
                Console.WriteLine($"   ✅ Ficheiro exportado com sucesso");
                if (!string.IsNullOrEmpty(ExcelFilePath))
                {
                    Console.WriteLine($"   • Localização: {ExcelFilePath}");
                }
            }
            else
            {
                Console.WriteLine($"   ❌ Erro na exportação");
            }
            Console.WriteLine();
        }
        
        // Final status
        Console.WriteLine("📋 ESTADO FINAL");
        var allSuccess = (!DatabaseOperationPerformed || (MembersNotProcessed == 0)) 
                        && (!ExcelOperationPerformed || ExcelExported);
        
        if (allSuccess)
        {
            Console.WriteLine("   ✅ Todas as operações concluídas com sucesso!");
        }
        else
        {
            Console.WriteLine("   ⚠️  Processo concluído com avisos ou erros");
        }
        
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }
}
