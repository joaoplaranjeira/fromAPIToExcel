using System.Text.Json.Serialization;

namespace fromAPIToExcel.Models.DTOs;

public class MemberDto
{
    [JsonPropertyName("memberCode")]
    public int MemberCode { get; set; } = 0;
    
    [JsonPropertyName("fullName")]
    public required string FullName { get; set; }
    
    [JsonPropertyName("birthDate")]
    public DateTime BirthDate { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("mobilePhone")]
    public string? MobilePhone { get; set; }
    
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("monthlyFee")]
    public decimal? MonthlyFee { get; set; }
    
    [JsonPropertyName("joinedUs")]
    public DateTime JoinedUs { get; set; }
    
    [JsonPropertyName("lastQuotaPaid")]
    public DateTime? LastQuotaPaid { get; set; }
    
    [JsonPropertyName("paymentLocal")]
    public string? PaymentLocal { get; set; }
    
    [JsonPropertyName("daysToBirthday")]
    public int DaysToBirthday { get; set; }
    
    [JsonPropertyName("yearsOfMembership")]
    public int YearsOfMembership { get; set; }
    
    [JsonPropertyName("age")]
    public int Age { get; set; }
}