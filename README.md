# From API to Excel/Database

Uma aplicação .NET 8 estruturada com Feature Driven Design (FDD) para extrair dados de membros de uma API e exportá-los para Excel ou inserir numa base de dados através de um endpoint.

## 🏗️ Arquitectura

A aplicação segue os princípios do Feature Driven Design, organizando o código por funcionalidades:

```
fromAPIToExcel/
├── Models/                          # Modelos de domínio
│   ├── Member.cs                   # Entidades principais (Member, Field, Id)
│   └── DTOs/                       # Data Transfer Objects
│       └── DatabaseDtos.cs         # DTOs para comunicação com base de dados
├── Infrastructure/                  # Camada de infraestrutura
│   ├── Configuration/              # Classes de configuração
│   │   └── AppSettings.cs
│   └── Services/                   # Serviços de infraestrutura
│       └── HttpRetryService.cs     # Serviço HTTP com retry automático
├── Features/                       # Funcionalidades organizadas por domínio
│   ├── MemberExtraction/           # Feature: Extração de membros
│   │   ├── Contracts/
│   │   │   └── IMemberExtractionService.cs
│   │   └── Services/
│   │       └── MemberExtractionService.cs  
│   ├── DatabaseInsertion/          # Feature: Inserção na base de dados
│   │   ├── Contracts/
│   │   │   └── IDatabaseInsertionService.cs
│   │   └── Services/
│   │       └── DatabaseInsertionService.cs
│   └── ExcelExport/               # Feature: Exportação para Excel
│       ├── Contracts/
│       │   └── IExcelExportService.cs
│       └── Services/
│           └── ExcelExportService.cs
├── Application/                    # Orquestração da aplicação
│   ├── Contracts/
│   │   └── IApplicationService.cs
│   └── Services/
│       └── ApplicationService.cs
├── appsettings.json               # Configuração da aplicação
└── Program.cs                     # Ponto de entrada com DI
```

## 🚀 Funcionalidades

### 1. **Extração de Membros** 
- Extrai dados de membros de uma API paginada
- Retry automático com delay de 2 segundos em caso de falha
- Logs em Português (Portugal)
- Configuração flexível de atributos

### 2. **Inserção na Base de Dados**
- Envia dados para um endpoint através de POST requests
- Processamento em lotes configurável
- Retry automático para pedidos falhados
- Validação e tratamento de erros

### 3. **Exportação para Excel**
- Gera ficheiros Excel com formatação automática
- Tipos de dados inteligentes (datas, números, texto)
- Filtros automáticos nos cabeçalhos
- Ajuste automático de largura das colunas

## ⚙️ Configuração

Configure a aplicação através do ficheiro `appsettings.json`:

```json
{
  "Api": {
    "MembersBaseUrl": "https://your-api.com/members",
    "CookieValue": "your-cookie-here",
    "PageSize": 100,
    "DelayBetweenRequests": 1000,
    "DelayBetweenDetails": 1000
  },
  "Database": {
    "InsertEndpoint": "https://your-database-api.com/api/members/batch",
    "ApiKey": "your-api-key",
    "BatchSize": 100
  },
  "Excel": {
    "OutputFileName": "membros.xlsx",
    "WorksheetName": "Membros"
  },
  "MemberAttributes": [
    "socio", "email", "phone", "category"
  ],
  "DetailAttributes": [
    "gender", "user_email", "phone"
  ]
}
```

## 🏃‍♂️ Como Usar

### Exportar apenas para Excel (padrão):
```bash
dotnet run
```

### Inserir na base de dados:
```bash
dotnet run --database
```

### Ambas as operações:
```bash
dotnet run --database
```

### Apenas base de dados (sem Excel):
```bash
dotnet run --database --no-excel
```

## 📦 Dependências

- **Microsoft.Extensions.*** - Dependency Injection, Configuration, Logging
- **ClosedXML** - Geração de ficheiros Excel
- **Newtonsoft.Json** - Serialização JSON

## 🔧 Desenvolvimento

### Adicionar Nova Funcionalidade

1. Criar pasta na `Features/` (ex: `Features/EmailNotification/`)
2. Adicionar contratos em `Contracts/`
3. Implementar serviços em `Services/`
4. Registar no DI em `Program.cs`
5. Integrar no `ApplicationService`

### Personalizar Retry Logic

O `HttpRetryService` pode ser estendido para diferentes estratégias de retry:

```csharp
// Configurar timeouts, número de tentativas, backoff exponencial, etc.
```

## 🐛 Tratamento de Erros

- **Logs estruturados** com diferentes níveis (Info, Warning, Error)
- **Retry automático** para falhas de rede
- **Processamento em lotes** para evitar sobrecarga
- **Validação** de configurações e dados

## 🔒 Segurança

- Cookies e API keys configuráveis
- Não hardcoding de credenciais
- Logs que não expõem informação sensível

## 📈 Performance

- **Processamento assíncrono** para múltiplas operações
- **Delays configuráveis** para evitar rate limiting
- **Processamento em lotes** para APIs de base de dados
- **Retry inteligente** com backoff

---

**Desenvolvido com Feature Driven Design para máxima manutenibilidade e escalabilidade.**