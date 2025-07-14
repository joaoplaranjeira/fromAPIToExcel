using System.Net.Http.Headers;
using System.Text.Json;
using ClosedXML.Excel;

public class Field
{
    public string attribute { get; set; }
    public object value { get; set; }
}

public class Id
{
    public string attribute { get; set; }
    public int value { get; set; }
}

public class Resource
{
    public Id id { get; set; }
    public List<Field> fields { get; set; }
    public string title { get; set; }
}

public class Root
{
    public string label { get; set; }
    public List<Resource> resources { get; set; }
    public string prev_page_url { get; set; }
    public string next_page_url { get; set; }
    public int total { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        List<string> attributes = new List<string>
        {
            "last_paid_quote",
            "birthdate",
            "athlete",
            "category",
            "subscription_date",
            "payment_local"
        };
        int page = 1;
        string apiUrl = string.Empty;
        string cookieValue = "cookie=XSRF-TOKEN=eyJpdiI6ImNORXcyWDY3cXcwMk0xdzRxckkvWVE9PSIsInZhbHVlIjoiK0VrUWhQak9iNlcrVzQ1Y2xzY003RmtUU08vUStjK0x2VHFzam95eWdaSDc1cmlHRWt1T256cmpqcVRFazVUUXE4UW1QRGY3Q2RaTXg1UWNsZlI1RjkvTklQUWM2cjlsMUs2V1MxTE9TRWJWSzNuUUtUZGRvVTdtMzVpaGNINkEiLCJtYWMiOiI4M2VkYjE3MzQ5Njg0MzRlMDczNmIxMjViMDEzODUxYzQ0NTFkYTNmNmJiMzQ4ZGUwMDJlZmE0MTA3MjU0YmZhIiwidGFnIjoiIn0%3D; leca_futebol_clube_area_de_membros_session=eyJpdiI6Im55VTBMSmVSN09NV09zb2dsaWRNb1E9PSIsInZhbHVlIjoiWnhXZUI2cng3WVdjWFRMK2tXOEVPZkNkY2xUT2ZGTkFPeU5iZWdrUjZ0Vm95SjdFVnJzeXpqZW42UEthMkJnV1QxM1NsZEZxZFZUUEZXampMOHZCM3FwVEJNT1RZUGcvY2dsT2NCbHdVVlBhaUFRZEFLT0NJR1M3bUZFRzZRVW8iLCJtYWMiOiI3MDU2OTA5NzJhNWFiMWY2NjIyZmUxMGFlMjI5ODA5NTBmZmYyNzVmMGIwYTY2NjNhNjE0N2NiZGM4ZWVmOWQ0IiwidGFnIjoiIn0%3D";           // <-- Substituir
        bool hasRecords = true;
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", cookieValue);

        var allResources = new List<Resource>();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        while (hasRecords)
        {
            apiUrl = $"https://members.lecafutebolclube.com/nova-api/members?search=&orderBy=socio&orderByDirection=asc&perPage=100&trashed=&page={page}&relationshipType=";
            Console.WriteLine($"🔄 A obter página {page}...");
            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonSerializer.Deserialize<Root>(json, options);

            if (root != null && root.resources != null)
            {
                allResources.AddRange(root.resources);
                page++;

                if (!string.IsNullOrEmpty(root.next_page_url))
                {
                    Console.WriteLine("⏳ A aguardar 5 segundos...");
                    await Task.Delay(5000);
                }
                else
                {
                    hasRecords = false;
                }
            }
            else
            {
                Console.WriteLine("⚠️ Resposta inválida ou sem recursos.");
                break;
            }
        }

        ExportToExcel(allResources, attributes);
        Console.WriteLine("✅ Excel gerado com sucesso!");
    }

    static void ExportToExcel(List<Resource> resources, List<string> attributes)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Resources");

        // Cabeçalhos
        worksheet.Cell(1, 1).Value = "Nr Socio | Nome";

        for (int i = 0; i < attributes.Count; i++)
        {
            worksheet.Cell(1, i + 3).Value = attributes[i];
        }

        // Dados
        for (int i = 0; i < resources.Count; i++)
        {
            var r = resources[i];
            worksheet.Cell(i + 2, 1).Value = r.title.Replace("#", ""); ;

            for (int j = 0; j < attributes.Count; j++)
            {
                var field = r.fields.FirstOrDefault(f => f.attribute == attributes[j]);
                worksheet.Cell(i + 2, j + 3).Value = field?.value?.ToString();
            }
        }

        workbook.SaveAs("saida.xlsx");
    }
}