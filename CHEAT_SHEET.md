# APBD Cheat Sheet

## Setup (do this first)

```bash
git clone https://github.com/f1scord/apbd-test-template
cd apbd-test-template
rm -rf .git
git init
git add -A
git commit -m "initial"
```
Then on github.com → New repository (empty). In Rider: **Git → Manage Remotes → + → paste URL → Push**

- [ ] `appsettings.json` → change `Initial Catalog=master` to exam DB name
- [ ] Rename controller, route, DTOs, IDbService to match exam entities
- [ ] `dotnet build` → 0 errors before writing any logic

---

## HTTP Status Codes

| Code | When |
|------|------|
| 200 OK | GET found record |
| 201 Created | POST success |
| 400 Bad Request | validation failed |
| 404 Not Found | record or FK not found |
| 409 Conflict | duplicate (record already exists) |

```csharp
return Ok(result);       // 200
return Created();        // 201
return BadRequest("msg");// 400
return NotFound("msg");  // 404
return Conflict("msg");  // 409
```

---

## GET Pattern

```csharp
var query = """
    SELECT m.Id AS RootId, m.Name AS RootName,
           p.Id AS ChildId, p.Name AS ChildName,
           pt.Id AS TypeId, pt.Name AS TypeName,
           v.Code AS VendorCode, v.Name AS VendorName, vp.Amount, vp.PricePerUnit
    FROM Makers m
    JOIN Products p     ON p.MakerId = m.Id
    JOIN ProductTypes pt ON pt.Id = p.ProductTypeId
    JOIN VendorProducts vp ON vp.ProductId = p.Id
    JOIN Vendors v      ON v.Code = vp.VendorCode
    WHERE m.Id = @Id;
    """;

await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync();
await using var command = new SqlCommand(query, connection);
command.Parameters.AddWithValue("@Id", id);
await using var reader = await command.ExecuteReaderAsync();

var ordRootId    = reader.GetOrdinal("RootId");
var ordChildId   = reader.GetOrdinal("ChildId");
var ordVendCode  = reader.GetOrdinal("VendorCode");
// ... add all ordinals

GetRootDto? result = null;

while (await reader.ReadAsync())
{
    if (result is null)
        result = new GetRootDto { Id = reader.GetInt32(ordRootId), Name = reader.GetString(ordRootName) };

    var childId = reader.GetInt32(ordChildId);
    var child = result.Children.FirstOrDefault(c => c.Id == childId);
    if (child is null)
    {
        child = new GetChildDto
        {
            Id   = childId,
            Type = new GetEmbeddedDto { Id = reader.GetInt32(ordTypeId), Name = reader.GetString(ordTypeName) },
        };
        result.Children.Add(child);
    }

    child.Vendors.Add(new GetLeafDto
    {
        Code         = reader.GetString(ordVendCode),
        PricePerUnit = reader.GetDecimal(ordPricePerUnit),
    });
}

return result ?? throw new NotFoundException($"Not found: {id}");
```

---

## POST Pattern

```csharp
await using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();
await using var command = new SqlCommand();
command.Connection = connection;
command.Transaction = transaction as SqlTransaction;

try
{
    // Insert root, get new PK (only if table has IDENTITY)
    command.CommandText = "INSERT INTO Makers (Name) VALUES (@Name); SELECT @@IDENTITY;";
    command.Parameters.AddWithValue("@Name", dto.Name);
    var rootId = Convert.ToInt32(await command.ExecuteScalarAsync());

    foreach (var item in dto.Items)
    {
        // Check FK by name → get id
        command.Parameters.Clear();
        command.CommandText = "SELECT Id FROM ProductTypes WHERE Name = @Name;";
        command.Parameters.AddWithValue("@Name", item.TypeName);
        var typeId = await command.ExecuteScalarAsync()
            ?? throw new NotFoundException($"Type '{item.TypeName}' not found.");

        // Insert child
        command.Parameters.Clear();
        command.CommandText = "INSERT INTO Products (Name, Description, StickerPrice, ProductTypeId, MakerId) VALUES (@Name, @Desc, @Price, @TypeId, @RootId);";
        command.Parameters.AddWithValue("@Name",   item.Name);
        command.Parameters.AddWithValue("@Desc",   (object?)item.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@Price",  item.Price);
        command.Parameters.AddWithValue("@TypeId", typeId);
        command.Parameters.AddWithValue("@RootId", rootId);
        await command.ExecuteNonQueryAsync();
    }

    await transaction.CommitAsync(); // AFTER the loop, not inside!
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## Nullable

```csharp
// Reading nullable column:
string? desc = reader.IsDBNull(ordDesc) ? null : reader.GetString(ordDesc);
DateTime? dt = reader.IsDBNull(ordDt)   ? null : reader.GetDateTime(ordDt);

// Writing nullable param:
command.Parameters.AddWithValue("@Desc", (object?)dto.Description ?? DBNull.Value);
```

---

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| `CommitAsync()` inside foreach | Move it **after** the loop |
| Forgot `Parameters.Clear()` | Call before every new query |
| `ExecuteNonQueryAsync` for INSERT returning id | Use `ExecuteScalarAsync` + `SELECT @@IDENTITY` |
| Ordinals inside while loop | Fetch them **before** the loop |
| Nullable column without `IsDBNull` | Always check before `GetString`, `GetDateTime` etc. |

---

## Reader Types

| C# type | Method |
|---------|--------|
| `int` | `reader.GetInt32(ord)` |
| `string` | `reader.GetString(ord)` |
| `decimal` | `reader.GetDecimal(ord)` |
| `DateTime` | `reader.GetDateTime(ord)` |
| `bool` | `reader.GetBoolean(ord)` |
| nullable | `reader.IsDBNull(ord) ? null : reader.GetXxx(ord)` |

---

## DI

```csharp
// Program.cs
builder.Services.AddScoped<IDbService, DbService>();
```

## Connection String (appsettings.json)

```json
"DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=YOUR_DB;Integrated Security=True;Encrypt=False;"
```
