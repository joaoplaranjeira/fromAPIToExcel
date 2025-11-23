using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using fromAPIToExcel.Features.DatabaseInsertion.Contracts;
using fromAPIToExcel.Infrastructure.Configuration;
using fromAPIToExcel.Infrastructure.Services;
using fromAPIToExcel.Models;
using fromAPIToExcel.Models.DTOs;

namespace fromAPIToExcel.Features.DatabaseInsertion.Services;

public class DatabaseInsertionService : IDatabaseInsertionService
{
    private readonly HttpRetryService _httpRetryService;
    private readonly ILogger<DatabaseInsertionService> _logger;
    private readonly AppSettings _settings;

    public DatabaseInsertionService(
        HttpRetryService httpRetryService,
        ILogger<DatabaseInsertionService> logger,
        IOptions<AppSettings> settings)
    {
        _httpRetryService = httpRetryService;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<DatabaseInsertResponse> InsertMembersAsync(List<Member> members)
    {
        _logger.LogInformation("🗄️ A iniciar inserção na base de dados. Total de membros: {Count}", members.Count);

        // Step 1: Fetch existing members from database with full data
        _logger.LogInformation("🔍 A verificar membros existentes na base de dados...");
        var existingMembers = await GetExistingMembersAsync();
        
        if (existingMembers.Any())
        {
            _logger.LogInformation("📋 Membros existentes na base de dados: {Count}", existingMembers.Count);
        }

        // Step 2: Convert members to DTOs
        var memberDtos = ConvertMembersToDto(members);
        
        // Step 3: Separate new members and members that need updates
        var existingMemberDict = existingMembers.ToDictionary(m => m.MemberCode);
        var newMembers = new List<MemberDto>();
        var membersToUpdate = new List<MemberDto>();
        int membersWithoutChanges = 0;
        
        foreach (var memberDto in memberDtos)
        {
            if (existingMemberDict.TryGetValue(memberDto.MemberCode, out var existingMember))
            {
                // Member exists - check if data changed
                var changedFields = GetChangedFields(memberDto, existingMember);
                if (changedFields.Any())
                {
                    membersToUpdate.Add(memberDto);
                    _logger.LogInformation("🔄 Membro {MemberCode} - {FullName} tem alterações: {Fields}", 
                        memberDto.MemberCode, memberDto.FullName, string.Join(", ", changedFields));
                }
                else
                {
                    membersWithoutChanges++;
                }
            }
            else
            {
                // New member
                newMembers.Add(memberDto);
            }
        }
        
        _logger.LogInformation("📊 Análise dos membros:");
        _logger.LogInformation("   ✨ Membros novos: {Count}", newMembers.Count);
        _logger.LogInformation("   🔄 Membros a atualizar: {Count}", membersToUpdate.Count);
        _logger.LogInformation("   ⏭️ Membros sem alterações: {Count}", membersWithoutChanges);
        
        var overallResponse = new DatabaseInsertResponse
        {
            Content = new DatabaseInsertResponseContent
            {
                MembersInserted = new List<MemberInsertResult>(),
                MembersUpdated = new List<MemberInsertResult>(),
                MembersNotInserted = new List<MemberInsertResult>()
            },
            MembersAlreadyInDatabase = existingMembers.Count,
            MembersWithoutChanges = membersWithoutChanges
        };

        // Step 4: Process new members (insert)
        if (newMembers.Count > 0)
        {
            await ProcessInsertBatchesAsync(newMembers, overallResponse);
        }
        
        // Step 5: Process updated members (update)
        if (membersToUpdate.Count > 0)
        {
            await ProcessUpdateBatchesAsync(membersToUpdate, overallResponse);
        }

        // Log final results
        _logger.LogInformation("📊 Resultado final:");
        _logger.LogInformation("   ✅ Membros inseridos: {Inserted}", overallResponse.TotalInserted);
        _logger.LogInformation("   🔄 Membros atualizados: {Updated}", overallResponse.TotalUpdated);
        _logger.LogInformation("   ❌ Membros não processados: {NotInserted}", overallResponse.TotalNotInserted);
        _logger.LogInformation("   📈 Total processado: {Total}", overallResponse.TotalProcessed);
        
        if (overallResponse.Content.MembersNotInserted.Any())
        {
            _logger.LogInformation("🔍 Detalhes dos membros não processados:");
            foreach (var notInserted in overallResponse.Content.MembersNotInserted.Take(10))
            {
                _logger.LogInformation("   • {MemberCode} - {FullName}: {Reason}", 
                    notInserted.MemberCode, notInserted.FullName, notInserted.Reason);
            }
            
            if (overallResponse.Content.MembersNotInserted.Count > 10)
            {
                _logger.LogInformation("   ... e mais {Count} membros não processados", 
                    overallResponse.Content.MembersNotInserted.Count - 10);
            }
        }

        return overallResponse;
    }

    private List<MemberDto> ConvertMembersToDto(List<Member> members)
    {
        return members.Select(member => new MemberDto
        {
            MemberCode = GetIntFieldValue(member, "socio"),
            FullName = CleanFullName(member.Title),
            BirthDate = ParseDateField(member, "birthdate", DateTime.MinValue),
            Email = GetFieldValue(member, "user_email"),
            MobilePhone = GetFieldValue(member, "phone"),
            Address = null, // Not available in current API
            Gender = GetFieldValue(member, "gender"),
            Type = GetFieldValue(member, "category"),
            MonthlyFee = ParseDecimalField(member, "monthly_fee"),
            JoinedUs = ParseDateField(member, "subscription_date", DateTime.Now),
            LastQuotaPaid = ParseNullableDateField(member, "last_paid_quote"),
            PaymentLocal = GetFieldValue(member, "payment_local")
        }).ToList();
    }

    private string? GetFieldValue(Member member, string attributeName)
    {
        var field = member.Fields.FirstOrDefault(f => f.Attribute == attributeName);
        return field?.Value?.ToString();
    }

    private int GetIntFieldValue(Member member, string attributeName, int defaultValue = 0)
    {
        var value = GetFieldValue(member, attributeName);
        if (string.IsNullOrEmpty(value)) return defaultValue;
        
        if (int.TryParse(value, out var intValue))
            return intValue;
            
        _logger.LogWarning("⚠️ Não foi possível converter o valor '{Value}' para inteiro no membro {MemberId}", value, member.Id?.Value ?? 0);
        return defaultValue;
    }

    private DateTime ParseDateField(Member member, string attributeName, DateTime defaultValue)
    {
        var value = GetFieldValue(member, attributeName);
        if (string.IsNullOrEmpty(value)) return defaultValue;
        
        if (DateTime.TryParse(value, out var dateValue))
            return dateValue;
            
        _logger.LogWarning("⚠️ Não foi possível converter a data '{Value}' para o membro {MemberId}", value, member.Id.Value);
        return defaultValue;
    }

    private DateTime? ParseNullableDateField(Member member, string attributeName)
    {
        var value = GetFieldValue(member, attributeName);
        if (string.IsNullOrEmpty(value)) return null;
        
        if (DateTime.TryParse(value, out var dateValue))
            return dateValue;
            
        _logger.LogWarning("⚠️ Não foi possível converter a data '{Value}' para o membro {MemberId}", value, member.Id.Value);
        return null;
    }

    private decimal? ParseDecimalField(Member member, string attributeName)
    {
        var value = GetFieldValue(member, attributeName);
        if (string.IsNullOrEmpty(value)) return null;
        
        // Handle different decimal formats (comma and dot)
        value = value.Replace(",", ".");
        
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Number, 
            System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
            return decimalValue;
            
        _logger.LogWarning("⚠️ Não foi possível converter o valor '{Value}' para decimal no membro {MemberId}", value, member.Id.Value);
        return null;
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

    private async Task<List<MemberDto>> GetExistingMembersAsync()
    {
        try
        {
            var response = await _httpRetryService.GetWithRetryAsync<GetAllMembersResponse>(
                _settings.Database.GetAllEndpoint, 
                "obter membros existentes");
            
            if (response?.Success == true && response.Content != null)
            {
                return response.Content;
            }
            
            _logger.LogWarning("⚠️ Não foi possível obter a lista de membros existentes. A continuar sem validação.");
            return new List<MemberDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter membros existentes da base de dados. A continuar sem validação.");
            return new List<MemberDto>();
        }
    }

    private bool HasMemberDataChanged(MemberDto newData, MemberDto existingData)
    {
        return newData.FullName != existingData.FullName ||
               newData.BirthDate != existingData.BirthDate ||
               newData.Email != existingData.Email ||
               newData.MobilePhone != existingData.MobilePhone ||
               newData.Address != existingData.Address ||
               newData.Gender != existingData.Gender ||
               newData.Type != existingData.Type ||
               newData.MonthlyFee != existingData.MonthlyFee ||
               newData.JoinedUs != existingData.JoinedUs ||
               newData.LastQuotaPaid != existingData.LastQuotaPaid ||
               newData.PaymentLocal != existingData.PaymentLocal;
    }

    private List<string> GetChangedFields(MemberDto newData, MemberDto existingData)
    {
        var changedFields = new List<string>();
        
        if (newData.FullName != existingData.FullName)
            changedFields.Add($"Nome: '{existingData.FullName}' → '{newData.FullName}'");
        
        if (newData.BirthDate != existingData.BirthDate)
            changedFields.Add($"Data Nascimento: '{existingData.BirthDate:yyyy-MM-dd}' → '{newData.BirthDate:yyyy-MM-dd}'");
        
        if (newData.Email != existingData.Email)
            changedFields.Add($"Email: '{existingData.Email}' → '{newData.Email}'");
        
        if (newData.MobilePhone != existingData.MobilePhone)
            changedFields.Add($"Telemóvel: '{existingData.MobilePhone}' → '{newData.MobilePhone}'");
        
        if (newData.Address != existingData.Address)
            changedFields.Add($"Morada: '{existingData.Address}' → '{newData.Address}'");
        
        if (newData.Gender != existingData.Gender)
            changedFields.Add($"Género: '{existingData.Gender}' → '{newData.Gender}'");
        
        if (newData.Type != existingData.Type)
            changedFields.Add($"Tipo: '{existingData.Type}' → '{newData.Type}'");
        
        if (newData.MonthlyFee != existingData.MonthlyFee)
            changedFields.Add($"Quota: '{existingData.MonthlyFee}' → '{newData.MonthlyFee}'");
        
        if (newData.JoinedUs != existingData.JoinedUs)
            changedFields.Add($"Data Inscrição: '{existingData.JoinedUs:yyyy-MM-dd}' → '{newData.JoinedUs:yyyy-MM-dd}'");
        
        if (newData.LastQuotaPaid != existingData.LastQuotaPaid)
            changedFields.Add($"Última Quota Paga: '{existingData.LastQuotaPaid:yyyy-MM-dd}' → '{newData.LastQuotaPaid:yyyy-MM-dd}'");
        
        if (newData.PaymentLocal != existingData.PaymentLocal)
            changedFields.Add($"Local Pagamento: '{existingData.PaymentLocal}' → '{newData.PaymentLocal}'");
        
        return changedFields;
    }

    private async Task ProcessInsertBatchesAsync(List<MemberDto> members, DatabaseInsertResponse overallResponse)
    {
        var totalBatches = (int)Math.Ceiling((double)members.Count / _settings.Database.BatchSize);
        _logger.LogInformation("📥 A processar inserção em {TotalBatches} lote(s)...", totalBatches);

        for (int i = 0; i < totalBatches; i++)
        {
            var batch = members.Skip(i * _settings.Database.BatchSize)
                              .Take(_settings.Database.BatchSize)
                              .ToList();

            _logger.LogInformation("📦 A inserir lote {CurrentBatch}/{TotalBatches} ({Count} membros)...", 
                i + 1, totalBatches, batch.Count);

            try
            {
                _logger.LogDebug("📋 Lote {BatchNumber}: Códigos {FirstCode}-{LastCode}", 
                    i + 1, batch.First().MemberCode, batch.Last().MemberCode);
                
                var response = await _httpRetryService.PostWithRetryAsync<List<MemberDto>, DatabaseInsertResponse>(
                    _settings.Database.InsertEndpoint, 
                    batch, 
                    $"inserir lote {i + 1}");

                if (response != null)
                {
                    overallResponse.Content.MembersInserted.AddRange(response.Content.MembersInserted);
                    overallResponse.Content.MembersNotInserted.AddRange(response.Content.MembersNotInserted);
                    
                    if (response.Content.MembersNotInserted.Any())
                    {
                        _logger.LogWarning("⚠️ Lote {BatchNumber}: {Inserted} inseridos, {NotInserted} falharam", 
                            i + 1, response.TotalInserted, response.TotalNotInserted);
                    }
                    else
                    {
                        _logger.LogInformation("✅ Lote {BatchNumber} inserido com sucesso ({Count} membros)", 
                            i + 1, response.TotalInserted);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao inserir lote {BatchNumber}", i + 1);
                
                foreach (var memberDto in batch)
                {
                    overallResponse.Content.MembersNotInserted.Add(new MemberInsertResult
                    {
                        MemberCode = memberDto.MemberCode,
                        FullName = memberDto.FullName,
                        Reason = $"Erro no lote: {ex.Message}"
                    });
                }
            }

            if (i < totalBatches - 1)
            {
                await Task.Delay(500);
            }
        }
    }

    private async Task ProcessUpdateBatchesAsync(List<MemberDto> members, DatabaseInsertResponse overallResponse)
    {
        var totalBatches = (int)Math.Ceiling((double)members.Count / _settings.Database.BatchSize);
        _logger.LogInformation("🔄 A processar atualização em {TotalBatches} lote(s)...", totalBatches);

        for (int i = 0; i < totalBatches; i++)
        {
            var batch = members.Skip(i * _settings.Database.BatchSize)
                              .Take(_settings.Database.BatchSize)
                              .ToList();

            _logger.LogInformation("📦 A atualizar lote {CurrentBatch}/{TotalBatches} ({Count} membros)...", 
                i + 1, totalBatches, batch.Count);

            try
            {
                _logger.LogDebug("📋 Lote {BatchNumber}: Códigos {FirstCode}-{LastCode}", 
                    i + 1, batch.First().MemberCode, batch.Last().MemberCode);
                
                var response = await _httpRetryService.PutWithRetryAsync<List<MemberDto>, DatabaseInsertResponse>(
                    _settings.Database.UpdateEndpoint, 
                    batch, 
                    $"atualizar lote {i + 1}");

                if (response != null)
                {
                    overallResponse.Content.MembersUpdated.AddRange(response.Content.MembersUpdated);
                    overallResponse.Content.MembersNotInserted.AddRange(response.Content.MembersNotInserted);
                    
                    if (response.Content.MembersNotInserted.Any())
                    {
                        _logger.LogWarning("⚠️ Lote {BatchNumber}: {Updated} atualizados, {NotUpdated} falharam", 
                            i + 1, response.TotalUpdated, response.TotalNotInserted);
                    }
                    else
                    {
                        _logger.LogInformation("✅ Lote {BatchNumber} atualizado com sucesso ({Count} membros)", 
                            i + 1, response.TotalUpdated);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao atualizar lote {BatchNumber}", i + 1);
                
                foreach (var memberDto in batch)
                {
                    overallResponse.Content.MembersNotInserted.Add(new MemberInsertResult
                    {
                        MemberCode = memberDto.MemberCode,
                        FullName = memberDto.FullName,
                        Reason = $"Erro na atualização: {ex.Message}"
                    });
                }
            }

            if (i < totalBatches - 1)
            {
                await Task.Delay(500);
            }
        }
    }
}