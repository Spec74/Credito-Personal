using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MorosidadReadService(IOptions<SqlDatabaseOptions> options)
    : IMorosidadReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<MorosidadEmpresaItemDto>> ObtenerMorososEmpresaAsync(
        string tipo = "TODOS",
        int? oficinaId = null,
        int? gestorId = null,
        DateTime? fechaCorte = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();

        var tipoNormalizado = string.IsNullOrWhiteSpace(tipo)
            ? "TODOS"
            : tipo.Trim().ToUpperInvariant();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<MorosidadEmpresaItemDto>(
            new CommandDefinition(
                "CREDITO.usp_MorosidadEmpresa",
                new
                {
                    Tipo = tipoNormalizado,
                    OficinaId = oficinaId,
                    UsuarioId = gestorId,
                    FechaCorte = fechaCorte?.Date,
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 90,
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
}
