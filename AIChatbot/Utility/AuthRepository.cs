using Npgsql;

namespace AIChatbot.Utility
{
    public sealed class AuthUser
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
    }

    public class AuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' was not found.");
        }

        public async Task<AuthUser?> FindByEmailAsync(string email)
        {
            await using var connection = await OpenConnectionAsync();
            const string sql = """
                SELECT id, email, password_hash
                FROM public.users
                WHERE email = @email
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("email", NormalizeEmail(email));
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new AuthUser
            {
                Id = reader.GetGuid(0),
                Email = reader.GetString(1),
                PasswordHash = reader.GetString(2)
            };
        }

        public async Task<AuthUser?> FindByIdAsync(Guid id)
        {
            await using var connection = await OpenConnectionAsync();
            const string sql = """
                SELECT id, email, password_hash
                FROM public.users
                WHERE id = @id
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", id);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new AuthUser
            {
                Id = reader.GetGuid(0),
                Email = reader.GetString(1),
                PasswordHash = reader.GetString(2)
            };
        }

        public async Task CreateAsync(string email, string passwordHash)
        {
            await using var connection = await OpenConnectionAsync();
            const string sql = """
                INSERT INTO public.users (id, email, password_hash)
                VALUES (@id, @email, @passwordHash)
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("email", NormalizeEmail(email));
            command.Parameters.AddWithValue("passwordHash", passwordHash);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();

                const string createTableSql = """
                    CREATE TABLE IF NOT EXISTS public.users
                    (
                        id uuid PRIMARY KEY,
                        email varchar(320) NOT NULL UNIQUE,
                        password_hash text NOT NULL,
                        created_at timestamptz NOT NULL DEFAULT now()
                    )
                    """;

                await using var command = new NpgsqlCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}
