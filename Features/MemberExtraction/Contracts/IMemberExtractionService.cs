using fromAPIToExcel.Models;

namespace fromAPIToExcel.Features.MemberExtraction.Contracts;

public interface IMemberExtractionService
{
    Task<ExtractionResult> ExtractAllMembersAsync(int? startFromMemberCode = null);
}