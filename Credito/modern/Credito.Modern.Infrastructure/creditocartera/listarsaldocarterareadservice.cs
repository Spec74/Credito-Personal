using System.Data;
using Credito.Modern.Application.CreditoCartera;
using Credito.Modern.Infrastructure.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoCartera;

public sealed class ListarSaldoCarteraReadService(IOptions<SqlDatabaseOptions> options)
    : IListarSaldoCarteraReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<ListarSaldoCarteraRowDto>> ListarAsync(
        int anio,
        int mes,
        int oficinaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (anio is < 1900 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(anio), "anio debe estar entre 1900 y 2100.");
        }

        if (mes is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(mes), "mes debe estar entre 1 y 12.");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_ListarSaldoCartera",
            new { Anio = anio, Mes = mes, OficinaId = oficinaId, UsuarioId = usuarioId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ListarSaldoCarteraRowDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
