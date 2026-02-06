using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Otw.Clevvo.App.Members.Import.Features.MemberExtraction.Contracts;
using Otw.Clevvo.App.Members.Import.Infrastructure.Configuration;
using Otw.Clevvo.App.Members.Import.Infrastructure.Services;
using Otw.Clevvo.App.Members.Import.Models;
using Otw.Clevvo.App.Members.Import.Models.DTOs;
using System.Text;
using System.Globalization;

namespace Otw.Clevvo.App.Members.Import.Features.MemberExtraction.Services;

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
                    AddPhotoBaseUrl(member);
                    
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
        
        // Apply gender deduction algorithm
        var genderDeductions = await DeduceGendersAsync(allMembers);
        
        return new ExtractionResult
        {
            Members = allMembers,
            PagesProcessed = page - 1,
            GenderDeductions = genderDeductions
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

    private void AddPhotoBaseUrl(Member member)
    {
        var photoField = member.Fields.FirstOrDefault(f => f.Attribute == "photo");
        if (photoField?.Value != null && !string.IsNullOrWhiteSpace(photoField.Value.ToString()))
        {
            var photoValue = photoField.Value.ToString()!;
            if (!photoValue.StartsWith("http"))
            {
                photoField.Value = $"https://members.lecafutebolclube.com/storage/{photoValue}";
            }
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

    private async Task<List<GenderDeductionRecord>> DeduceGendersAsync(List<Member> members)
    {
        var deductions = new List<GenderDeductionRecord>();
        
        // Get members without gender
        var membersWithoutGender = members
            .Where(m => string.IsNullOrWhiteSpace(GetFieldValue(m, "gender")))
            .ToList();
        
        if (!membersWithoutGender.Any())
        {
            _logger.LogInformation("ℹ️ Todos os membros já têm género definido. Nenhuma dedução necessária.");
            return deductions;
        }
        
        _logger.LogInformation("🔍 A iniciar dedução de género para {Count} membros sem género...", membersWithoutGender.Count);
        
        // Get existing members from database
        var existingMembers = await GetExistingMembersFromDatabaseAsync();
        
        if (!existingMembers.Any())
        {
            _logger.LogWarning("⚠️ Não foi possível obter membros da base de dados para dedução de género.");
            return deductions;
        }
        
        _logger.LogInformation("📊 Base de dados consultada: {Count} membros encontrados", existingMembers.Count);
        
        // Build lookup by normalized first name
        var genderByFirstName = BuildGenderLookup(existingMembers);
        
        foreach (var member in membersWithoutGender)
        {
            var firstName = GetFirstName(member.Title);
            if (string.IsNullOrWhiteSpace(firstName))
                continue;
            
            var normalizedFirstName = NormalizeName(firstName);
            
            // Try to find gender from lookup
            if (genderByFirstName.TryGetValue(normalizedFirstName, out var gender))
            {
                // Update member's gender field
                var genderField = member.Fields.FirstOrDefault(f => f.Attribute == "gender");
                if (genderField != null)
                {
                    genderField.Value = gender;
                }
                else
                {
                    member.Fields.Add(new Field { Attribute = "gender", Value = gender });
                }
                
                var memberCode = GetIntFieldValue(member, "socio");
                
                deductions.Add(new GenderDeductionRecord
                {
                    MemberCode = memberCode,
                    FullName = CleanFullName(member.Title),
                    DeducedGender = gender
                });
                
                _logger.LogDebug("✅ Género deduzido para {MemberCode} - {FullName}: {Gender}", 
                    memberCode, member.Title, gender);
            }
        }
        
        _logger.LogInformation("✅ Dedução de género concluída: {Count} géneros atribuídos", deductions.Count);
        
        return deductions;
    }

    private async Task<List<MemberDto>> GetExistingMembersFromDatabaseAsync()
    {
        try
        {
            var response = await _httpRetryService.GetWithRetryAsync<GetAllMembersResponse>(
                _settings.Database.GetAllEndpoint, 
                "obter membros existentes para dedução de género");
            
            if (response?.Success == true && response.Content != null)
            {
                return response.Content;
            }
            
            return new List<MemberDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter membros existentes da base de dados para dedução de género.");
            return new List<MemberDto>();
        }
    }

    private Dictionary<string, string> BuildGenderLookup(List<MemberDto> existingMembers)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var member in existingMembers.Where(m => !string.IsNullOrWhiteSpace(m.Gender)))
        {
            var firstName = GetFirstNameFromFullName(member.FullName);
            if (string.IsNullOrWhiteSpace(firstName))
                continue;
            
            var normalizedFirstName = NormalizeName(firstName);
            
            // Only add if not already in dictionary (first occurrence wins)
            if (!lookup.ContainsKey(normalizedFirstName))
            {
                lookup[normalizedFirstName] = member.Gender!;
            }
        }
        
        _logger.LogDebug("📋 Dicionário de géneros construído com {Count} entradas", lookup.Count);
        
        return lookup;
    }

    private string GetFirstName(string fullName)
    {
        var cleaned = CleanFullName(fullName);
        return GetFirstNameFromFullName(cleaned);
    }

    private string GetFirstNameFromFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;
        
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;
        
        // Remove accents
        var normalizedString = name.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Trim();
    }

    private string? GetFieldValue(Member member, string attributeName)
    {
        var field = member.Fields.FirstOrDefault(f => f.Attribute == attributeName);
        return field?.Value?.ToString();
    }

    private int GetIntFieldValue(Member member, string attributeName)
    {
        var value = GetFieldValue(member, attributeName);
        if (int.TryParse(value, out var result))
        {
            return result;
        }
        return 0;
    }

    private string CleanFullName(string title)
    {
        if (string.IsNullOrEmpty(title)) return string.Empty;

        // Remove the "#" character first
        var cleanTitle = title.Replace("#", "").Trim();

        // Check if it starts with a number followed by " - " pattern
        var parts = cleanTitle.Split(" - ", 2);
        if (parts.Length == 2 && int.TryParse(parts[0], out _))
        {
            // Return only the name part (after the number and hyphen)
            return parts[1].Trim();
        }

        // If no number prefix, return as is
        return cleanTitle;
    }
}