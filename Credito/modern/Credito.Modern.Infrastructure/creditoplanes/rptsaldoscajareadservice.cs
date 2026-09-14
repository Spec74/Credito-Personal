using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptSaldosCajaReadService(IOptions<SqlDatabaseOptions> options) : IRptSaldosCajaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<RptSaldosCajaRowDto>> ListarAsync(
        int cajaDiarioId,
        bool indCajaChica,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_RptSaldosCaja",
            new { CajaDiarioId = cajaDiarioId, IndCajaChica = indCajaChica },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RptSaldosCajaRowDto>(command).ConfigureAwait(false);
        return rows.Select(r => new RptSaldosCajaRowDto
        {
            MovimientoCajaId = r.MovimientoCajaId,
            Operacion = r.Operacion,
            FechaReg = r.FechaReg,
            Codigo = r.Codigo,
            Cliente = r.Cliente,
            ImportePago = r.ImportePago,
            IndEntrada = r.IndEntrada,
            Glosa = r.Glosa,
            TipoPago = r.TipoPago,
            EstadoActivo = true,
        }).ToList();
    }

    public async Task<List<RptSaldosCajaRowDto>> ListarArqueoAsync(
        int cajaDiarioId,
        bool incluirAnulados,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        if (!incluirAnulados)
        {
            return await ListarAsync(cajaDiarioId, indCajaChica: false, cancellationToken)
                .ConfigureAwait(false);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                MC.MovimientoCajaId,
                MC.Operacion,
                MC.FechaReg,
                P.Codigo,
                P.NombreCompleto AS Cliente,
                MC.ImportePago,
                MC.IndEntrada,
                MC.Descripcion AS Glosa,
                ISNULL(TP.Denominacion, '') AS TipoPago,
                MC.Estado AS EstadoActivo
            FROM CREDITO.MovimientoCaja AS MC
            LEFT JOIN MAESTRO.VALORTABLA AS TP
                ON TP.TablaId = 13 AND MC.TipoPagoId = TP.ItemId AND TP.ItemId > 0
            LEFT JOIN MAESTRO.Persona AS P ON MC.PersonaId = P.PersonaId
            WHERE MC.CajaDiarioId = @CajaDiarioId
            ORDER BY MC.FechaReg;
            """;
        var rows = await connection
            .QueryAsync<RptSaldosCajaRowDto>(
                new CommandDefinition(
                    sql,
                    new { CajaDiarioId = cajaDiarioId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
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
