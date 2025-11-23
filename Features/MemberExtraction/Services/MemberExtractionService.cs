using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using fromAPIToExcel.Features.MemberExtraction.Contracts;
using fromAPIToExcel.Infrastructure.Configuration;
using fromAPIToExcel.Infrastructure.Services;
using fromAPIToExcel.Models;

namespace fromAPIToExcel.Features.MemberExtraction.Services;

public class MemberExtractionService : IMemberExtractionService
{
    private readonly HttpRetryService _httpRetryService;
    private readonly ILogger<MemberExtractionService> _logger;
    private readonly AppSettings _settings;

    public MemberExtractionService(
        HttpRetryService httpRetryService, 
        ILogger<MemberExtractionService> logger,
        IOptions<AppSettings> settings)
    {
        _httpRetryService = httpRetryService;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<ExtractionResult> ExtractAllMembersAsync(int? startFromMemberCode = null)
    {
        _logger.LogInformation("🚀 A iniciar extração de membros...");
        
        // Determine the effective start code: command line overrides appsettings
        var effectiveStartCode = startFromMemberCode ?? _settings.Api.StartFromMemberCode;
        
        if (effectiveStartCode > 0 || _settings.Api.EndUntilMemberCode > 0)
        {
            var source = startFromMemberCode.HasValue ? "comando" : "configuração";
            if (effectiveStartCode > 0 && _settings.Api.EndUntilMemberCode > 0)
            {
                _logger.LogInformation("📌 Configurado para importar membros no intervalo [{StartCode} - {EndCode}] (fonte início: {Source})", 
                    effectiveStartCode, _settings.Api.EndUntilMemberCode, source);
            }
            else if (effectiveStartCode > 0)
            {
                _logger.LogInformation("📌 Configurado para importar apenas membros com código >= {StartCode} (fonte: {Source})", 
                    effectiveStartCode, source);
            }
            else if (_settings.Api.EndUntilMemberCode > 0)
            {
                _logger.LogInformation("📌 Configurado para importar apenas membros com código <= {EndCode}", 
                    _settings.Api.EndUntilMemberCode);
            }
        }
        
        if (_settings.Api.MaxMembersToExtract > 0)
        {
            _logger.LogInformation("📊 Limite máximo de extração: {MaxMembers} membros", _settings.Api.MaxMembersToExtract);
        }
        
        var allMembers = new List<Member>();
        int page = 1;
        bool hasRecords = true;

        while (hasRecords)
        {
            _logger.LogInformation("🔄 A obter página {Page}...", page);
            
            var apiUrl = $"{_settings.Api.MembersBaseUrl}?search=&orderBy=socio&orderByDirection=asc&perPage={_settings.Api.PageSize}&trashed=&page={page}&relationshipType=";
            
            var response = await _httpRetryService.GetWithRetryAsync<MemberResponse>(apiUrl, $"página {page}");
            
            if (response?.Resources != null && response.Resources.Any())
            {
                _logger.LogInformation("📄 Página {Page}: {Count} membros encontrados (Total na API: {Total})", 
                    page, response.Resources.Count, response.Total);
                _logger.LogDebug("🔗 NextPageUrl: {NextPageUrl}", response.NextPageUrl ?? "null");
                // Filter members based on effective StartFromMemberCode (comparing with "socio" field)
                var filteredMembers = response.Resources
                    .Where(m => IsMemberCodeValid(m, effectiveStartCode))
                    .ToList();

                if (filteredMembers.Count < response.Resources.Count)
                {
                    var intervalMsg = _settings.Api.EndUntilMemberCode > 0 
                        ? $"intervalo [{effectiveStartCode} - {_settings.Api.EndUntilMemberCode}]"
                        : $"código >= {effectiveStartCode}";
                    _logger.LogInformation("🔍 Filtrados {Filtered} de {Total} membros ({Interval})", 
                        filteredMembers.Count, response.Resources.Count, intervalMsg);
                }

                // Apply max members limit before processing details
                if (_settings.Api.MaxMembersToExtract > 0 && _settings.Api.MaxMembersToExtract < filteredMembers.Count)
                {
                    _logger.LogInformation("🔒 A limitar membros desta página para respeitar o máximo de {MaxMembers}", 
                            _settings.Api.MaxMembersToExtract);
                        filteredMembers = filteredMembers.Take(_settings.Api.MaxMembersToExtract).ToList();
                }

                // Process filtered members and get details if needed
                foreach (var member in filteredMembers)
                {
                    if (_settings.DetailAttributes.Any() &&
                        member.Fields.FirstOrDefault(f => f.Attribute == "category")?.Value?.ToString() != "Atleta")
                    {
                        await GetMemberDetailsAsync(member);
                        _logger.LogDebug("⏳ A aguardar {Delay}ms para obter o detalhe...", _settings.Api.DelayBetweenDetails);
                        await Task.Delay(_settings.Api.DelayBetweenDetails);
                    }
                }

                allMembers.AddRange(filteredMembers);
                page++;

                // Check if we have more records to fetch
                var totalFetched = allMembers.Count;
                
                // Check if max limit reached
                if (_settings.Api.MaxMembersToExtract > 0 && totalFetched >= _settings.Api.MaxMembersToExtract)
                {
                    _logger.LogInformation("🛑 Limite máximo de {MaxMembers} membros atingido. A parar extração.", _settings.Api.MaxMembersToExtract);
                    hasRecords = false;
                    continue;
                }
                
                var hasMoreRecords = !string.IsNullOrEmpty(response.NextPageUrl) || 
                                   (response.Total > totalFetched && response.Resources.Count == _settings.Api.PageSize);

                if (hasMoreRecords)
                {
                    _logger.LogDebug("⏳ A aguardar {Delay}ms... (Total obtido: {Fetched}/{Total})", 
                        _settings.Api.DelayBetweenRequests, totalFetched, response.Total);
                    await Task.Delay(_settings.Api.DelayBetweenRequests);
                }
                else
                {
                    _logger.LogInformation("🏁 Extração concluída na página {Page}. Total obtido: {Fetched}/{Total}", 
                        page - 1, totalFetched, response.Total);
                    hasRecords = false;
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Resposta inválida ou sem recursos na página {Page}.", page);
                hasRecords = false;
            }
        }

        _logger.LogInformation("✅ Extração concluída. Total de membros: {Count}", allMembers.Count);
        
        return new ExtractionResult
        {
            Members = allMembers,
            PagesProcessed = page - 1
        };
    }

    private async Task GetMemberDetailsAsync(Member member)
    {
        var detailUrl = $"{_settings.Api.MembersBaseUrl}/{member.Id.Value}";
        
        var detailResponse = await _httpRetryService.GetWithRetryAsync<MemberDetailResponse>(
            detailUrl, $"recurso {member.Id.Value}");
            
        if (detailResponse?.Resource?.Fields != null)
        {
            var additionalFields = detailResponse.Resource.Fields
                .Where(f => _settings.DetailAttributes.Contains(f.Attribute));
            
            member.Fields.AddRange(additionalFields);
        }
    }

    private bool IsMemberCodeValid(Member member, int effectiveStartCode)
    {
        var socioField = member.Fields.FirstOrDefault(f => f.Attribute == "socio");
        if (socioField?.Value != null && int.TryParse(socioField.Value.ToString(), out var socioValue))
        {
            var meetsStartRequirement = socioValue >= effectiveStartCode;
            var meetsEndRequirement = _settings.Api.EndUntilMemberCode == 0 || socioValue <= _settings.Api.EndUntilMemberCode;
            return meetsStartRequirement && meetsEndRequirement;
        }
        return false; // Exclude members without valid socio field
    }
}