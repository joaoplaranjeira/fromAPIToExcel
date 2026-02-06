using Otw.Clevvo.App.Members.Import.Models;
using Otw.Clevvo.App.Members.Import.Models.DTOs;

namespace Otw.Clevvo.App.Members.Import.Features.DatabaseInsertion.Contracts;

public interface IDatabaseInsertionService
{
    Task<DatabaseInsertResponse> InsertMembersAsync(List<Member> members);
}