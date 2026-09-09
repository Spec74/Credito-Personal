using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CreditoSolicitudWriteService(IOptions<SqlDatabaseOptions> options) : ICreditoSolicitudWriteService
{
    private const decimal MontoCreditoInicial = 500m;
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<CrearSolicitudCreditoResponse> CrearSolicitudAsync(
        int oficinaId,
        int personaId,
        int usuarioId,
        DateTime fechaReg,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || personaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var solicitudExistenteId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP (1) CreditoId
                    FROM CREDITO.Credito
                    WHERE PersonaId = @PersonaId
                      AND OficinaId = @OficinaId
                      AND Estado = 'CRE'
                      AND EsPrendario = CAST(0 AS bit)
                    ORDER BY CreditoId DESC;
                    """,
                    new { PersonaId = personaId, OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (solicitudExistenteId is > 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new CrearSolicitudCreditoResponse(solicitudExistenteId.Value);
            }

            var personaAvalId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP (1) PersonaAvalId
                    FROM CREDITO.Credito
                    WHERE PersonaId = @PersonaId
                      AND Estado NOT IN ('CRE', 'ANU')
                    ORDER BY CreditoId DESC;
                    """,
                    new { PersonaId = personaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var montoGastosAdm = await CalcularGastosAdmAsync(
                connection,
                transaction,
                MontoCreditoInicial,
                incluyeCentralRiesgo: true,
                cancellationToken).ConfigureAwait(false);

            var solicitudId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.Credito (
                        PersonaId, ProductoId, Descripcion, MontoProducto, MontoInicial, MontoCredito,
                        MontoGastosAdm, CentralRiesgo, MontoDesembolso, TipoGastoAdm, FormaPago, NumeroCuotas,
                        Interes, FechaPrimerPago, Observacion, Estado, OficinaId, FechaVencimiento, TipoCuota,
                        Calificacion, IndCondonacion, MontoCondonacion, IndIrrecuperable, PersonaAvalId,
                        UsuarioRegId, FechaReg)
                    VALUES (
                        @PersonaId, 1, '', 0, 0, @MontoCredito,
                        @MontoGastosAdm, 0, 0, 'CAP', 'D', 26,
                        8, @FechaReg, '', 'CRE', @OficinaId, @FechaReg, 'F',
                        'A', 0, 0, 0, @PersonaAvalId,
                        @UsuarioRegId, @FechaReg);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        PersonaId = personaId,
                        MontoCredito = MontoCreditoInicial,
                        MontoGastosAdm = montoGastosAdm,
                        OficinaId = oficinaId,
                        FechaReg = fechaReg.Date,
                        PersonaAvalId = personaAvalId,
                        UsuarioRegId = usuarioId,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CrearSolicitudCreditoResponse(solicitudId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Alta de solicitud prendaria. Paridad <c>CreditoBL.CrearSolicitudCreditoPrendario</c>: los
    /// valores fijos los ajusta después el analista en el simulador. A diferencia del legacy, que
    /// crea una solicitud nueva en cada llamada, aquí se reutiliza la solicitud prendaria en
    /// estado CRE que ya tenga la persona, igual que en la solicitud ordinaria.
    /// </summary>
    public async Task<CrearSolicitudCreditoResponse> CrearSolicitudPrendariaAsync(
        int oficinaId,
        int personaId,
        int usuarioId,
        DateTime fechaReg,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || personaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "Parámetros inválidos.");
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var solicitudExistenteId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP (1) CreditoId
                    FROM CREDITO.Credito
                    WHERE PersonaId = @PersonaId
                      AND OficinaId = @OficinaId
                      AND Estado = 'CRE'
                      AND EsPrendario = CAST(1 AS bit)
                    ORDER BY CreditoId DESC;
                    """,
                    new { PersonaId = personaId, OficinaId = oficinaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (solicitudExistenteId is > 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new CrearSolicitudCreditoResponse(solicitudExistenteId.Value);
            }

            var personaAvalId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP (1) PersonaAvalId
                    FROM CREDITO.Credito
                    WHERE PersonaId = @PersonaId
                      AND Estado NOT IN ('CRE', 'ANU')
                    ORDER BY CreditoId DESC;
                    """,
                    new { PersonaId = personaId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var montoGastosAdm = await CalcularGastosAdmAsync(
                connection,
                transaction,
                MontoCreditoInicial,
                incluyeCentralRiesgo: true,
                cancellationToken).ConfigureAwait(false);

            var solicitudId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.Credito (
                        PersonaId, ProductoId, Descripcion, MontoProducto, MontoInicial, MontoCredito,
                        MontoGastosAdm, CentralRiesgo, MontoDesembolso, TipoGastoAdm, FormaPago, NumeroCuotas,
                        Interes, FechaPrimerPago, Observacion, Estado, OficinaId, FechaVencimiento, TipoCuota,
                        Calificacion, IndCondonacion, MontoCondonacion, IndIrrecuperable, PersonaAvalId,
                        UsuarioRegId, FechaReg, EsPrendario)
                    VALUES (
                        @PersonaId, 2, 'CREDITO PRENDARIO', 0, 0, @MontoCredito,
                        @MontoGastosAdm, 0, 0, 'CAP', 'M', 1,
                        8, @FechaVencimiento, '', 'CRE', @OficinaId, @FechaVencimiento, 'F',
                        'A', 0, 0, 0, @PersonaAvalId,
                        @UsuarioRegId, @FechaReg, CAST(1 AS bit));
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        PersonaId = personaId,
                        MontoCredito = MontoCreditoInicial,
                        MontoGastosAdm = montoGastosAdm,
                        OficinaId = oficinaId,
                        FechaReg = fechaReg.Date,
                        FechaVencimiento = fechaReg.Date.AddMonths(1),
                        PersonaAvalId = personaAvalId,
                        UsuarioRegId = usuarioId,
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CrearSolicitudCreditoResponse(solicitudId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<decimal> CalcularGastosAdmAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        decimal importe,
        bool incluyeCentralRiesgo,
        CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync<GastosAdmRow>(
            new CommandDefinition(
                """
                SELECT Ind, IndPorcentaje, Valor
                FROM CREDITO.GastosAdm
                WHERE Estado = CAST(1 AS bit)
                  AND @Importe >= MontoMinimo
                  AND @Importe <= MontoMaximo;
                """,
                new { Importe = importe },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var filtered = incluyeCentralRiesgo
            ? rows
            : rows.Where(x => !x.Ind);

        decimal gastos = 0;
        foreach (var item in filtered)
        {
            gastos += item.IndPorcentaje ? importe * (item.Valor / 100m) : item.Valor;
        }

        return Math.Round(gastos, 2);
    }

    private sealed class GastosAdmRow
    {
        public bool Ind { get; init; }
        public bool IndPorcentaje { get; init; }
        public decimal Valor { get; init; }
    }
}
