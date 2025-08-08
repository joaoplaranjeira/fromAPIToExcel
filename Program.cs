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

public class RootDetail
{
    public string title { get; set; }
    public Resource resource { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        List<string> attributes = new List<string>
        {
            "socio",
            "last_paid_quote",
            "birthdate",
            "athlete",
            "category",
            "subscription_date",
            "payment_local",
            "gender",
            "monthly_fee"
        };

        List<string> attributesDetails = new List<string>
        {
            //"gender"
        };

        int page = 1;
        string apiUrl = string.Empty;
        string cookieValue = "XSRF-TOKEN=eyJpdiI6IjFhcXNmUWxreG1KdEV2elRlZzQvYmc9PSIsInZhbHVlIjoiSUthL01ZbEJPMjdSUkNNS0RCK3dUeTV5WEVQQTdGQzRyS2lyQlVkd1pjeTQzZzhkZEdYRkFuMTBsTDlnN0dlblJDWUdBL0pZLzVHWXE2c21hcnYva1FKaXJQUGF3ZlFKOG0zY21tTGxqcVJNeGd6bWhscmxDaUpFUUUyN05hSTciLCJtYWMiOiIxZjYzYTZkOGNiMTgyMDcwMWNjZjc4MDUxZjgwZmQ4MGEzNTkxYTFiZTViOGEzMWQ0NDFiMDJlZjUxMWRjMDUyIiwidGFnIjoiIn0%3D; leca_futebol_clube_area_de_membros_session=eyJpdiI6IndxclkrVXhvNFFJbGtoL1NoU3ZFd1E9PSIsInZhbHVlIjoiVlVja3piWFF2UVp3djEyL1pxbDBxZzI3b0E2MTJNaFAvT2lBaDk4N3NPdnBua0pKRDNJNm5MbDEwQzVjMzFZUHRvMVpCQlhzeUxNNmVvQnpHL1BNRjEvZFdISDlIaEl5YWZ4emIxSHhYV0ErRTRoeDNUSVBwc0JoVnFwN2VGengiLCJtYWMiOiI0MWIzYzk3ZjhmZDg5NzcyZWU5MjMwNmIzMGJmNDc5OWI3ZDlhN2Y1NWUzYjhjMWE2MWQzNTA1MDIwODQ3YjRhIiwidGFnIjoiIn0%3D; _ga_4XLXQY2C2R=GS2.1.s1753806386$o1$g1$t1753806624$j56$l0$h0; _ga=GA1.1.722021716.1753806387";           // <-- Substituir
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
                foreach (var resource in root.resources)
                {
                    if (attributesDetails != null && attributesDetails.Count() > 0)
                    {
                        // Obter detalhes de cada recurso
                    var resourceDetails = await GetResourceDetails(client, resource.id.value);
                    if (resourceDetails != null)
                    {
                        resource.fields.AddRange(resourceDetails.fields.Where(f => attributesDetails.Contains(f.attribute)));
                    }
                    Console.WriteLine("⏳ A aguardar 1 segundos para obter o detalhe...");
                    await Task.Delay(1000);    
                    }
                }

                allResources.AddRange(root.resources);
                page++;

                if (!string.IsNullOrEmpty(root.next_page_url))
                {
                    Console.WriteLine("⏳ A aguardar 2 segundos...");
                    await Task.Delay(2000);
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

    static async Task<Resource> GetResourceDetails(HttpClient client, int resourceId)
    {
        var detailUrl = $"https://members.lecafutebolclube.com/nova-api/members/{resourceId}";
        var response = await client.GetAsync(detailUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var rootDetail = JsonSerializer.Deserialize<RootDetail>(json);

        return rootDetail?.resource;
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