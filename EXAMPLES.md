# Exemplos de Uso

## 1. Configurar Endpoint de Base de Dados

Edite o `appsettings.json` para configurar o endpoint da sua base de dados:

```json
{
  "Database": {
    "InsertEndpoint": "https://sua-api.exemplo.com/api/membros/inserir",
    "ApiKey": "Bearer seu-token-aqui",
    "BatchSize": 50
  }
}
```

## 2. Estrutura Esperada do Endpoint

O endpoint deve aceitar POST requests com a seguinte estrutura:

### Request:
```json
{
  "members": [
    {
      "memberCode": 123,
      "fullName": "João Silva",
      "birthDate": "1985-03-15T00:00:00",
      "email": "joao@exemplo.com",
      "mobilePhone": "+351912345678",
      "address": null,
      "gender": "M",
      "type": "Sénior",
      "monthlyFee": 25.50,
      "joinedUs": "2020-01-15T00:00:00",
      "lastQuotaPaid": "2024-10-01T00:00:00",
      "paymentLocal": "Sede"
    }
  ]
}
```

### Response Esperada:
```json
{
  "success": true,
  "message": "Membros inseridos com sucesso",
  "processedCount": 1,
  "errors": []
}
```

### Response com Erros:
```json
{
  "success": false,
  "message": "Alguns membros não foram inseridos",
  "processedCount": 0,
  "errors": [
    "Membro com código 123 já existe",
    "Email inválido para membro 456",
    "Data de nascimento inválida para membro 789"
  ]
}
```

## 3. Executar Diferentes Cenários

### Apenas Excel (comportamento padrão):
```bash
dotnet run
```

### Apenas Base de Dados:
```bash
dotnet run --database --no-excel
```

### Excel + Base de Dados:
```bash
dotnet run --database
```

## 4. Personalizar Configurações

### Alterar atributos extraídos:
```json
{
  "MemberAttributes": [
    "socio",
    "user_email", 
    "phone",
    "category",
    "subscription_date"
  ]
}
```

### Configurar delays:
```json
{
  "Api": {
    "DelayBetweenRequests": 2000,  // 2 segundos entre páginas
    "DelayBetweenDetails": 1500    // 1.5 segundos entre detalhes
  }
}
```

### Configurar tamanho dos lotes:
```json
{
  "Database": {
    "BatchSize": 25  // Inserir 25 membros por vez
  }
}
```

## 5. Logs de Exemplo

```
🚀 A iniciar processamento de membros...
🎯 Operações selecionadas:
   ✓ Exportação para Excel
   ✓ Inserção na base de dados
🚀 A iniciar extração de membros...
🔄 A obter página 1...
🔄 A obter página 2...
✅ Extração concluída. Total de membros: 150
🗄️ A iniciar inserção na base de dados. Total de membros: 150
📦 A processar lote 1/2 (100 membros)...
✅ Lote 1 inserido com sucesso (100 membros)
📦 A processar lote 2/2 (50 membros)...
✅ Lote 2 inserido com sucesso (50 membros)
✅ Inserção na base de dados concluída com sucesso! Processados: 150
📊 A iniciar exportação para Excel. Total de membros: 150
✅ Excel exportado com sucesso: membros.xlsx
🎉 Processamento concluído com sucesso!
```

## 6. Implementação do Endpoint (Exemplo Node.js/Express)

```javascript
app.post('/api/membros/inserir', async (req, res) => {
  try {
    const { members } = req.body;
    let processedCount = 0;
    const errors = [];

    for (const member of members) {
      try {
        // Validate required fields
        if (!member.fullName || !member.fullName.trim()) {
          errors.push(`Nome completo é obrigatório para o membro ${member.memberCode}`);
          continue;
        }

        if (!member.birthDate || new Date(member.birthDate) === 'Invalid Date') {
          errors.push(`Data de nascimento inválida para o membro ${member.memberCode}`);
          continue;
        }

        await database.insertMember({
          memberCode: member.memberCode,
          fullName: member.fullName.trim(),
          birthDate: new Date(member.birthDate),
          email: member.email,
          mobilePhone: member.mobilePhone,
          address: member.address,
          gender: member.gender,
          type: member.type,
          monthlyFee: member.monthlyFee,
          joinedUs: new Date(member.joinedUs),
          lastQuotaPaid: member.lastQuotaPaid ? new Date(member.lastQuotaPaid) : null,
          paymentLocal: member.paymentLocal
        });
        
        processedCount++;
      } catch (error) {
        errors.push(`Erro no membro ${member.memberCode}: ${error.message}`);
      }
    }

    res.json({
      success: errors.length === 0,
      message: errors.length === 0 ? 'Membros inseridos com sucesso' : 'Alguns erros ocorreram',
      processedCount,
      errors
    });
  } catch (error) {
    res.status(500).json({
      success: false,
      message: 'Erro interno do servidor',
      processedCount: 0,
      errors: [error.message]
    });
  }
});
```