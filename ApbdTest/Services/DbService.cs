using ApbdTest.DTOs;
using ApbdTest.Exceptions;
using Microsoft.Data.SqlClient;

namespace ApbdTest.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<GetRootDto> GetAsync(int id)
    {
        var query = """
                    SELECT
                        -- TODO: columns with aliases
                        -- e.g. c.first_name AS FirstName,
                        --      r.rental_id  AS RentalId,
                        --      m.title      AS MovieTitle
                    FROM MainTable c
                    -- TODO: JOINs
                    WHERE c.Id = @Id;
                    """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync();

        GetRootDto? result = null;

        // TODO: declare ordinals for every column
        // var ordField1  = reader.GetOrdinal("Field1");
        // var ordChildId = reader.GetOrdinal("ChildId");
        // var ordLeafName = reader.GetOrdinal("LeafName");

        while (await reader.ReadAsync())
        {
            if (result is null)
            {
                result = new GetRootDto()
                {
                    // TODO: map root fields
                    // Field1 = reader.GetString(ordField1),
                    Children = new List<GetChildDto>()
                };
            }

            // TODO: deduplicate children by id
            // var childId = reader.GetInt32(ordChildId);
            // var child = result.Children.FirstOrDefault(e => e.Id.Equals(childId));

            // if (child is null)
            // {
            //     child = new GetChildDto
            //     {
            //         Id   = childId,
            //         // TODO: map other child fields
            //         // NullableDate = reader.IsDBNull(ordNullableDate) ? null : reader.GetDateTime(ordNullableDate),
            //         Items = new List<GetLeafDto>()
            //     };
            //     result.Children.Add(child);
            // }

            // TODO: always add leaf item
            // child.Items.Add(new GetLeafDto
            // {
            //     Name  = reader.GetString(ordLeafName),
            //     Price = reader.GetDecimal(ordPrice)
            // });
        }

        return result ?? throw new NotFoundException("Not found.");
    }

    public async Task CreateAsync(int id, CreateRootDto dto)
    {
        var insertRootQuery = """
                              -- TODO: INSERT INTO MainTable (...) VALUES (...);
                              SELECT @@IDENTITY;
                              """;

        var insertItemQuery = """
                              -- TODO: INSERT INTO ItemTable (...) VALUES (...);
                              """;

        var checkFkQuery = """
                           -- TODO: SELECT fk_id FROM FkTable WHERE name = @Name;
                           """;

        var checkExistsQuery = """
                               SELECT 1
                               FROM MainTable
                               WHERE Id = @Id;
                               """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        await using var command = new SqlCommand();
        command.Connection = connection;
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = checkExistsQuery;
            command.Parameters.AddWithValue("@Id", id);

            var existsRes = await command.ExecuteScalarAsync();
            if (existsRes == null)
            {
                throw new NotFoundException($"Not found: {id}");
            }

            command.Parameters.Clear();
            command.CommandText = insertRootQuery;
            // TODO: add parameters, e.g.:
            // command.Parameters.AddWithValue("@Date", dto.Date);
            // command.Parameters.AddWithValue("@Id", id);

            var rootObject = await command.ExecuteScalarAsync();
            var rootId = Convert.ToInt32(rootObject);

            foreach (var item in dto.Items)
            {
                command.Parameters.Clear();
                command.CommandText = checkFkQuery;
                command.Parameters.AddWithValue("@Name", item.Name);

                var fkObject = await command.ExecuteScalarAsync();
                if (fkObject == null)
                {
                    throw new NotFoundException($"Not found: {item.Name}");
                }

                var fkId = Convert.ToInt32(fkObject);

                command.Parameters.Clear();
                command.CommandText = insertItemQuery;
                // TODO: add parameters, e.g.:
                // command.Parameters.AddWithValue("@RootId", rootId);
                // command.Parameters.AddWithValue("@FkId",   fkId);
                // command.Parameters.AddWithValue("@Price",  item.Price);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
