using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaMovReadService(IOptions<SqlDatabaseOptions> options) : IBovedaMovReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<CajaAbiertaTransferenciaRowDto>> ListarCajasAbiertasParaTransferenciaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                cd.CajaId,
                c.Denominacion + N' - ' + u.NombreUsuario AS Etiqueta,
                cd.UsuarioAsignadoId
            FROM CREDITO.CajaDiario AS cd
            INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioAsignadoId
            WHERE c.OficinaId = @OficinaId
              AND cd.IndCierre = CAST(0 AS bit)
            ORDER BY c.Denominacion;
            """;
        var rows = await connection
            .QueryAsync<CajaAbiertaTransferenciaRowDto>(
                new CommandDefinition(sql, new { OficinaId = oficinaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.ToList();
    }
}
