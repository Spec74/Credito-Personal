using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Infrastructure.Auth;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class ConfirmarClaveCajaDiarioService(IOptions<SqlDatabaseOptions> options)
    : IConfirmarClaveCajaDiarioService
{
    private const string AdminUsuario = "ADMVENDIX";
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<ConfirmarClaveCajaDiarioResponse> VerificarAsync(
        string clave,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var claveAlmacenada = await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                """
                SELECT TOP 1 ClaveUsuario
                FROM MAESTRO.Usuario
                WHERE NombreUsuario = @NombreUsuario;
                """,
                new { NombreUsuario = AdminUsuario },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (string.IsNullOrEmpty(claveAlmacenada)
            || !UsuarioPasswordHasher.Verify(claveAlmacenada, clave ?? string.Empty, out _))
        {
            return new ConfirmarClaveCajaDiarioResponse(false, "NO AUTORIZADO!!!!");
        }

        return new ConfirmarClaveCajaDiarioResponse(true, null);
    }
}
