using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptSaldoCajaCabReadService(IOptions<SqlDatabaseOptions> options)
    : IRptSaldoCajaCabReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<RptSaldoCajaCabDto?> ObtenerAsync(
        int cajaDiarioId,
        bool cajaChica,
        CancellationToken cancellationToken = default)
    {
        if (cajaDiarioId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = cajaChica
            ? """
              SELECT TOP 1
                     o.Denominacion AS Oficina,
                     u.NombreUsuario + N' - ' + p.NombreCompleto + N' - CAJA CHICA' AS Cajero,
                     CASE WHEN cc.IndCierre = CAST(1 AS bit) THEN N'CERRADO' ELSE N'ABIERTO' END AS Estado,
                     cc.FechaIniOperacion AS Fecha,
                     cc.SaldoInicial,
                     cc.SaldoFinal,
                     CAST(0 AS decimal(18, 2)) AS PorcentajeCobro
              FROM CREDITO.CajaChicaDiario AS cc
              INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cc.UsuarioId
              INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
              CROSS APPLY (
                  SELECT TOP 1 ofi.Denominacion
                  FROM CREDITO.Caja AS c
                  INNER JOIN MAESTRO.Oficina AS ofi ON ofi.OficinaId = c.OficinaId
                  WHERE c.Denominacion LIKE N'%CAJA CHICA%'
                  ORDER BY c.CajaId
              ) AS o
              WHERE cc.Id = @Id;
              """
            : """
              SELECT o.Denominacion AS Oficina,
                     u.NombreUsuario + N' - ' + p.NombreCompleto + N' - ' + c.Denominacion AS Cajero,
                     CASE WHEN cd.IndCierre = CAST(1 AS bit) THEN N'CERRADO' ELSE N'ABIERTO' END AS Estado,
                     cd.FechaIniOperacion AS Fecha,
                     cd.SaldoInicial,
                     cd.SaldoFinal,
                     CASE
                         WHEN cd.MontoPorCobrar > 0
                         THEN (cd.MontoCobrado * 100) / cd.MontoPorCobrar
                         ELSE CAST(0 AS decimal(18, 2))
                     END AS PorcentajeCobro
              FROM CREDITO.CajaDiario AS cd
              INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
              INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = c.OficinaId
              INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioAsignadoId
              INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
              WHERE cd.CajaDiarioId = @Id;
              """;

        return await connection
            .QueryFirstOrDefaultAsync<RptSaldoCajaCabDto>(
                new CommandDefinition(sql, new { Id = cajaDiarioId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
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
