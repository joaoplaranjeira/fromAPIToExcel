namespace fromAPIToExcel.Models;

public class ExtractionResult
{
    public List<Member> Members { get; set; } = new();
    public int PagesProcessed { get; set; }
}
