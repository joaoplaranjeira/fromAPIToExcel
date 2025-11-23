using fromAPIToExcel.Models;
using fromAPIToExcel.Models.DTOs;

namespace fromAPIToExcel.Features.DatabaseInsertion.Contracts;

public interface IDatabaseInsertionService
{
    Task<DatabaseInsertResponse> InsertMembersAsync(List<Member> members);
}