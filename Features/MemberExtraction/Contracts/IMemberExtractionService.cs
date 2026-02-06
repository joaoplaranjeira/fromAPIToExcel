using Otw.Clevvo.App.Members.Import.Models;

namespace Otw.Clevvo.App.Members.Import.Features.MemberExtraction.Contracts;

public interface IMemberExtractionService
{
    Task<ExtractionResult> ExtractAllMembersAsync(int? startFromMemberCode = null);
}