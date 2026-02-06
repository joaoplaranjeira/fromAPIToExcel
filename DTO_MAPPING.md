# DTO Mapping Summary

## Updated MemberDto Structure

The application now uses the correct DTO structure expected by your members API:

```csharp
public class MemberDto
{
    public int MemberCode { get; set; } = 0;
    public required string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string? Email { get; set; }
    public string? MobilePhone { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? Type { get; set; }
    public decimal? MonthlyFee { get; set; }
    public DateTime JoinedUs { get; set; }
    public DateTime? LastQuotaPaid { get; set; }
    public string? PaymentLocal { get; set; }
    public string? PhotoUrl { get; set; }
}
```

## Field Mapping

| API Field | DTO Property | Data Type | Conversion Notes |
|-----------|--------------|-----------|------------------|
| `id.value` | `MemberCode` | `int` | Direct mapping |
| `title` | `FullName` | `string` | Removes "#" characters |
| `birthdate` | `BirthDate` | `DateTime` | Parsed with validation |
| `user_email` | `Email` | `string?` | Optional field |
| `phone` | `MobilePhone` | `string?` | Optional field |
| N/A | `Address` | `string?` | Not available in source API |
| `gender` | `Gender` | `string?` | Optional field |
| `category` | `Type` | `string?` | Maps category to type |
| `monthly_fee` | `MonthlyFee` | `decimal?` | Handles comma/dot separators |
| `subscription_date` | `JoinedUs` | `DateTime` | Parsed with validation |
| `last_paid_quote` | `LastQuotaPaid` | `DateTime?` | Optional, parsed with validation |
| `payment_local` | `PaymentLocal` | `string?` | Optional field |
| `photo.previewUrl` | `PhotoUrl` | `string?` | Extracted from photo JSON field |

## Data Conversion Features

### 🗓️ **Date Parsing**
- Handles multiple date formats
- Provides default values for required dates
- Logs warnings for invalid dates
- Nullable dates return `null` for invalid values

### 💰 **Decimal Parsing**
- Handles both comma (`,`) and dot (`.`) decimal separators
- Uses invariant culture for consistent parsing
- Logs warnings for invalid decimal values
- Returns `null` for optional decimal fields

### 📝 **String Cleaning**
- Removes "#" characters from member names
- Trims whitespace from names
- Handles null/empty values gracefully

### 📷 **Photo URL Extraction**
- Parses the complex photo field JSON structure
- Extracts the `previewUrl` property
- Handles missing or invalid photo data gracefully
- Returns `null` if photo is not available

## Error Handling

The conversion process includes comprehensive error handling:

```csharp
// Date conversion with logging
private DateTime ParseDateField(Member member, string attributeName, DateTime defaultValue)
{
    var value = GetFieldValue(member, attributeName);
    if (string.IsNullOrEmpty(value)) return defaultValue;
    
    if (DateTime.TryParse(value, out var dateValue))
        return dateValue;
        
    _logger.LogWarning("⚠️ Não foi possível converter a data '{Value}' para o membro {MemberId}", value, member.Id.Value);
    return defaultValue;
}
```

## Example JSON Output

```json
{
  "members": [
    {
      "memberCode": 12345,
      "fullName": "João Silva",
      "birthDate": "1985-03-15T00:00:00",
      "email": "joao.silva@email.com",
      "mobilePhone": "+351912345678",
      "address": null,
      "gender": "M",
      "type": "Sénior",
      "monthlyFee": 25.50,
      "joinedUs": "2020-01-15T00:00:00",
      "lastQuotaPaid": "2024-10-01T00:00:00",
      "paymentLocal": "Sede",
      "photoUrl": "https://members.lecafutebolclube.com/storage/avatares/PY1xokjff7XPaN5urhPCergLrELjm3ERIn6ABsgL.jpg"
    }
  ]
}
```

## Validation in Your API

Your API endpoint should validate:

1. **Required Fields**: `MemberCode`, `FullName`, `BirthDate`, `JoinedUs`
2. **Data Types**: Ensure dates are valid, decimals are properly formatted
3. **Business Rules**: Check for duplicate member codes, valid email formats
4. **Optional Fields**: Handle null values gracefully

## Testing the Integration

Use these commands to test:

```bash
# Test with database insertion
dotnet run --database

# Check logs for conversion warnings
# Look for "⚠️ Não foi possível converter..." messages
```

The application will log any data conversion issues while still processing successfully convertible records.