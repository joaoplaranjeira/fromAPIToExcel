namespace fromAPIToExcel.Application.Contracts;

public interface IApplicationService
{
    Task RunAsync(bool exportToExcel = true, bool insertToDatabase = false, int? startFromMemberCode = null);
}