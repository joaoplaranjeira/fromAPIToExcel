namespace fromAPIToExcel.Infrastructure.Configuration;

public class ApiSettings
{
    public string MembersBaseUrl { get; set; } = string.Empty;
    public string CookieValue { get; set; } = string.Empty;
    public int PageSize { get; set; } = 100;
    public int StartFromMemberCode { get; set; } = 0;
    public int EndUntilMemberCode { get; set; } = 0;
    public int MaxMembersToExtract { get; set; } = 0;
    public int DelayBetweenRequests { get; set; } = 1000;
    public int DelayBetweenDetails { get; set; } = 1000;
}

public class DatabaseSettings
{
    public string GetAllEndpoint { get; set; } = string.Empty;
    public string InsertEndpoint { get; set; } = string.Empty;
    public string UpdateEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 100;
}

public class ExcelSettings
{
    public string OutputFileName { get; set; } = "saida.xlsx";
    public string WorksheetName { get; set; } = "Resources";
}

public class AppSettings
{
    public ApiSettings Api { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();
    public ExcelSettings Excel { get; set; } = new();
    public List<string> MemberAttributes { get; set; } = new();
    public List<string> DetailAttributes { get; set; } = new();
}