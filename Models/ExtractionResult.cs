namespace Otw.Clevvo.App.Members.Import.Models;

public class ExtractionResult
{
    public List<Member> Members { get; set; } = new();
    public int PagesProcessed { get; set; }
    public List<GenderDeductionRecord> GenderDeductions { get; set; } = new();
}
