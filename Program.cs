using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Otw.Clevvo.App.Members.Import.Application.Contracts;
using Otw.Clevvo.App.Members.Import.Application.Services;
using Otw.Clevvo.App.Members.Import.Features.MemberExtraction.Contracts;
using Otw.Clevvo.App.Members.Import.Features.MemberExtraction.Services;
using Otw.Clevvo.App.Members.Import.Features.DatabaseInsertion.Contracts;
using Otw.Clevvo.App.Members.Import.Features.DatabaseInsertion.Services;
using Otw.Clevvo.App.Members.Import.Features.ExcelExport.Contracts;
using Otw.Clevvo.App.Members.Import.Features.ExcelExport.Services;
using Otw.Clevvo.App.Members.Import.Infrastructure.Configuration;
using Otw.Clevvo.App.Members.Import.Infrastructure.Services;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        
        var serviceProvider = services.BuildServiceProvider();

        // Parse command line arguments
        var exportToExcel = !args.Contains("--no-excel");
        var insertToDatabase = args.Contains("--database") || args.Contains("--db");
        
        // Parse starting member code from command line
        int? startFromMemberCode = null;
        var startFromIndex = Array.FindIndex(args, arg => arg == "--start-from" || arg == "--from");
        if (startFromIndex >= 0 && startFromIndex + 1 < args.Length)
        {
            if (int.TryParse(args[startFromIndex + 1], out var memberCode))
            {
                startFromMemberCode = memberCode;
            }
            else
            {
                Console.WriteLine($"⚠️ Valor inválido para --start-from: {args[startFromIndex + 1]}");
                Environment.Exit(1);
            }
        }

        // Show help if requested
        if (args.Contains("--help") || args.Contains("-h"))
        {
            ShowHelp();
            return;
        }

        if (!exportToExcel && !insertToDatabase)
        {
            Console.WriteLine("⚠️ Nenhuma operação selecionada. Use --database para inserir na base de dados.");
            Console.WriteLine("   Por defeito, apenas a exportação para Excel é ativa.");
            Console.WriteLine("   Use --help para ver todas as opções disponíveis.");
            exportToExcel = true; // Default to Excel export
        }

        try
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("🎯 Operações selecionadas:");
            if (exportToExcel) logger.LogInformation("   ✓ Exportação para Excel");
            if (insertToDatabase) logger.LogInformation("   ✓ Inserção na base de dados");

            // Run the application
            var applicationService = serviceProvider.GetRequiredService<IApplicationService>();
            await applicationService.RunAsync(exportToExcel, insertToDatabase, startFromMemberCode);
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "❌ Erro crítico na aplicação");
            Environment.Exit(1);
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AppSettings>(configuration);

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // HttpClient
        services.AddHttpClient<HttpRetryService>();

        // Infrastructure Services
        services.AddScoped<HttpRetryService>();

        // Feature Services
        services.AddScoped<IMemberExtractionService, MemberExtractionService>();
        services.AddScoped<IDatabaseInsertionService, DatabaseInsertionService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();

        // Application Services
        services.AddScoped<IApplicationService, ApplicationService>();
    }

    private static void ShowHelp()
    {
        Console.WriteLine("🚀 OTW Clevvo - Importação de Membros");
        Console.WriteLine();
        Console.WriteLine("Utilização:");
        Console.WriteLine("  dotnet run [opções]");
        Console.WriteLine();
        Console.WriteLine("Opções:");
        Console.WriteLine("  --database, --db          Inserir dados na base de dados");
        Console.WriteLine("  --no-excel                Não exportar para Excel");
        Console.WriteLine("  --start-from <código>     Importar apenas membros com código >= valor especificado");
        Console.WriteLine("  --from <código>           Alias para --start-from");
        Console.WriteLine("  --help, -h                Mostrar esta ajuda");
        Console.WriteLine();
        Console.WriteLine("Exemplos:");
        Console.WriteLine("  dotnet run                               # Apenas exportação Excel (padrão)");
        Console.WriteLine("  dotnet run --database                    # Inserção na BD + Excel");
        Console.WriteLine("  dotnet run --database --no-excel        # Apenas inserção na BD");
        Console.WriteLine("  dotnet run --start-from 1100            # Importar membros >= 1100");
        Console.WriteLine("  dotnet run --database --from 500        # BD + membros >= 500");
        Console.WriteLine();
        Console.WriteLine("Nota: O valor --start-from substitui a configuração em appsettings.json");
    }
}