using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaChicaDiarioReadService(IOptions<SqlDatabaseOptions> options)
    : ICajaChicaDiarioReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CajaChicaSesionDto?> GetSesionAbiertaAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.QuerySingleOrDefaultAsync<CajaChicaSesionDto>(
            new CommandDefinition(
                """
                SELECT Id,
                       UsuarioId,
                       SaldoInicial,
                       Entradas,
                       Salidas,
                       SaldoFinal,
                       FechaIniOperacion,
                       IndCierre
                FROM CREDITO.CajaChicaDiario
                WHERE UsuarioId = @UsuarioId
                  AND IndCierre = CAST(0 AS bit);
                """,
                new { UsuarioId = usuarioId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MovimientoCajaChicaRowDto>> ListarMovimientosAsync(
        int usuarioId,
        string tipo,
        CancellationToken cancellationToken = default)
    {
        var sesion = await GetSesionAbiertaAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (sesion is null)
        {
            return Array.Empty<MovimientoCajaChicaRowDto>();
        }

        var indEntrada = string.Equals(tipo, "E", StringComparison.OrdinalIgnoreCase);

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<MovimientoCajaChicaRowDto>(
            new CommandDefinition(
                """
                SELECT mc.Id AS MovimientoCajaChicaId,
                       mc.CajaChicaDiarioId,
                       mc.FechaReg,
                       mc.IndEntrada,
                       ISNULL(p.NombreCompleto, '') AS Persona,
                       op.Denominacion AS Operacion,
                       mc.Descripcion,
                       mc.Importe AS ImportePago,
                       mc.Estado
                FROM CREDITO.MovimientoCajaChica AS mc
                INNER JOIN MAESTRO.TipoOperacion AS op ON op.Codigo = mc.Operacion
                LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = mc.PersonaId
                WHERE mc.CajaChicaDiarioId = @CajaChicaDiarioId
                  AND mc.IndEntrada = @IndEntrada
                ORDER BY mc.FechaReg DESC;
                """,
                new { CajaChicaDiarioId = sesion.Id, IndEntrada = indEntrada },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<RendicionPendienteRowDto>> ListarRendicionesPendientesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var sesion = await GetSesionAbiertaAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (sesion is null)
        {
            return Array.Empty<RendicionPendienteRowDto>();
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<RendicionPendienteRowDto>(
            new CommandDefinition(
                """
                SELECT mc.Id AS MovimientoCajaChicaId,
                       p.NombreCompleto AS Cliente,
                       mc.Descripcion,
                       mc.FechaReg,
                       mc.Importe,
                       mc.ImporteRendido
                FROM CREDITO.MovimientoCajaChica AS mc
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = mc.PersonaId
                WHERE mc.CajaChicaDiarioId = @CajaChicaDiarioId
                  AND mc.Operacion = 'GAS'
                  AND mc.Estado = CAST(1 AS bit)
                  AND mc.IndRendido = CAST(0 AS bit)
                ORDER BY mc.FechaReg DESC;
                """,
                new { CajaChicaDiarioId = sesion.Id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<RendicionComprobanteRowDto>> ListarRendicionesAsync(
        int movimientoCajaChicaId,
        CancellationToken cancellationToken = default)
    {
        if (movimientoCajaChicaId < 1)
        {
            return Array.Empty<RendicionComprobanteRowDto>();
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<RendicionComprobanteRowDto>(
            new CommandDefinition(
                """
                SELECT r.Id,
                       r.MovimientoCajaChicaId,
                       r.RUC AS Ruc,
                       r.RazonSocial,
                       r.DetalleGasto,
                       td.Denominacion AS TipoDocumento,
                       r.Fecha,
                       r.Serie,
                       r.Numero,
                       r.Importe
                FROM CREDITO.MovimientoRendidoCajaChica AS r
                INNER JOIN MAESTRO.TipoDocumento AS td ON td.TipoDocumentoId = r.TipoDocumentoId
                WHERE r.MovimientoCajaChicaId = @MovimientoCajaChicaId
                ORDER BY r.Fecha DESC, r.Id DESC;
                """,
                new { MovimientoCajaChicaId = movimientoCajaChicaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<int> ContarRendicionesPendientesAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var sesion = await GetSesionAbiertaAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (sesion is null)
        {
            return 0;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM CREDITO.MovimientoCajaChica
                WHERE CajaChicaDiarioId = @CajaChicaDiarioId
                  AND Operacion = 'GAS'
                  AND IndRendido = CAST(0 AS bit);
                """,
                new { CajaChicaDiarioId = sesion.Id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<decimal?> GetSaldoFinalSesionAbiertaAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var sesion = await GetSesionAbiertaAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        return sesion?.SaldoFinal;
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
