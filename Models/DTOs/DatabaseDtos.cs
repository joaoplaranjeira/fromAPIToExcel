using System.Text.Json.Serialization;

namespace fromAPIToExcel.Models.DTOs;

public class DatabaseInsertRequest
{
    public List<MemberDto> Members { get; set; } = new();
}

public class MemberInsertResult
{
    [JsonPropertyName("memberCode")]
    public int MemberCode { get; set; }
    
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class DatabaseInsertResponseContent
{
    [JsonPropertyName("membersInserted")]
    public List<MemberInsertResult> MembersInserted { get; set; } = new();
    
    [JsonPropertyName("membersNotInserted")]
    public List<MemberInsertResult> MembersNotInserted { get; set; } = new();
    
    [JsonPropertyName("membersUpdated")]
    public List<MemberInsertResult> MembersUpdated { get; set; } = new();
}

public class DatabaseInsertResponse
{
    [JsonPropertyName("result")]
    public bool Result { get; set; }
    
    [JsonPropertyName("content")]
    public DatabaseInsertResponseContent Content { get; set; } = new();
    
    // Tracking properties (not from API response)
    public int MembersAlreadyInDatabase { get; set; }
    public int MembersWithoutChanges { get; set; }
    
    // Helper properties for backward compatibility and easier processing
    public bool Success => Result;
    public int TotalInserted => Content.MembersInserted.Count;
    public int TotalUpdated => Content.MembersUpdated.Count;
    public int TotalNotInserted => Content.MembersNotInserted.Count;
    public int TotalProcessed => TotalInserted + TotalUpdated + TotalNotInserted;
}

public class GetAllMembersResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("content")]
    public List<MemberDto> Content { get; set; } = new();
}