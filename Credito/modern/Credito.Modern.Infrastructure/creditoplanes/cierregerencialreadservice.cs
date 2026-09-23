using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CierreGerencialReadService(IOptions<SqlDatabaseOptions> options)
    : ICierreGerencialReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<AvanceMetaGerencialDto>> ObtenerAvanceAsync(
        DateTime periodo,
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "La oficina indicada no es válida.");
        }

        var periodoNormalizado = PrimerDia(periodo);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<AvanceMetaGerencialDto>(
            new CommandDefinition(
                "CREDITO.usp_ObtenerAvanceMetasGerenciales",
                new { Periodo = periodoNormalizado, OficinaId = oficinaId },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 180,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.AsList();
    }

    public async Task<IReadOnlyList<MetaGerencialDefinitivaDto>> ListarMetasAsync(
        DateTime periodo,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        var periodoNormalizado = PrimerDia(periodo);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<MetaGerencialDefinitivaDto>(
            new CommandDefinition(
                "CREDITO.usp_ListarMetasGerencialesDefinitivas",
                new { Periodo = periodoNormalizado },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 180,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.AsList();
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private static DateTime PrimerDia(DateTime periodo) =>
        new(periodo.Year, periodo.Month, 1);
}
