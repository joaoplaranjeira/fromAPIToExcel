namespace fromAPIToExcel.Models;

public class Field
{
    public string Attribute { get; set; } = string.Empty;
    public object? Value { get; set; }
}

public class Id
{
    public string Attribute { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class Member
{
    public Id Id { get; set; } = new();
    public List<Field> Fields { get; set; } = new();
    public string Title { get; set; } = string.Empty;
}

public class MemberResponse
{
    public string Label { get; set; } = string.Empty;
    public List<Member> Resources { get; set; } = new();
    public string? PrevPageUrl { get; set; }
    public string? NextPageUrl { get; set; }
    public int Total { get; set; }
}

public class MemberDetailResponse
{
    public string Title { get; set; } = string.Empty;
    public Member Resource { get; set; } = new();
}