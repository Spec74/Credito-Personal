using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class MovimientoCajaTicketReadService(
    IOptions<SqlDatabaseOptions> options,
    ICuotasPendientesReadService cuotasPendientes,
    IFechaOperativaReadService fechaOperativa)
    : IMovimientoCajaTicketReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<MovimientoCajaTicketDto?> ObtenerAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (movimientoCajaId < 1)
        {
            return null;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var header = await connection.QueryFirstOrDefaultAsync<TicketHeaderRow>(
            new CommandDefinition(
                """
                SELECT
                    mc.MovimientoCajaId,
                    mc.Operacion,
                    mc.CreditoId,
                    mc.PersonaId,
                    mc.ImportePago,
                    mc.Descripcion,
                    mc.IndEntrada,
                    mc.FechaReg,
                    mc.TipoPagoId,
                    ISNULL(p.NombreCompleto, '') AS Cliente,
                    u.NombreUsuario AS [User],
                    o.Denominacion AS Oficina,
                    ISNULL(pr.Denominacion, mc.Descripcion) AS Producto,
                    COALESCE(vt.Denominacion, CONCAT('Medio ', mc.TipoPagoId)) AS TipoPago,
                    c.NumeroCuotas,
                    c.Estado AS EstadoCreditoRaw,
                    c.IndCondonacion,
                    c.MontoCondonacion,
                    mc.OrdenVentaId
                FROM CREDITO.MovimientoCaja AS mc
                INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
                INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
                INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = cj.OficinaId
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = mc.UsuarioRegId
                LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = mc.PersonaId
                LEFT JOIN CREDITO.Credito AS c ON c.CreditoId = mc.CreditoId
                LEFT JOIN CREDITO.Producto AS pr ON pr.ProductoId = c.ProductoId
                LEFT JOIN MAESTRO.ValorTabla AS vt
                    ON vt.TablaId = 13 AND vt.ItemId = mc.TipoPagoId
                WHERE mc.MovimientoCajaId = @MovimientoCajaId;
                """,
                new { MovimientoCajaId = movimientoCajaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (header is null)
        {
            return null;
        }

        return header.Operacion switch
        {
            "CUO" => await BuildCuotaAsync(connection, header, cancellationToken).ConfigureAwait(false),
            "INI" or "GAD" or "CDN" => BuildInicial(header),
            "CON" => await BuildContadoAsync(connection, header, cancellationToken).ConfigureAwait(false),
            _ => BuildOtros(header),
        };
    }

    private async Task<MovimientoCajaTicketDto> BuildCuotaAsync(
        SqlConnection connection,
        TicketHeaderRow header,
        CancellationToken cancellationToken)
    {
        var tieneCuotaPagada = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM CREDITO.PlanPago
                    WHERE MovimientoCajaId = @MovimientoCajaId AND Estado = 'PAG'
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { header.MovimientoCajaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return tieneCuotaPagada
            ? await BuildCuotaCreditoAsync(connection, header, cancellationToken).ConfigureAwait(false)
            : await BuildCuotaLibreAsync(connection, header, cancellationToken).ConfigureAwait(false);
    }

    private async Task<MovimientoCajaTicketDto> BuildCuotaCreditoAsync(
        SqlConnection connection,
        TicketHeaderRow header,
        CancellationToken cancellationToken)
    {
        var agg = await connection.QueryFirstAsync<CuotaCreditoAggRow>(
            new CommandDefinition(
                """
                SELECT
                    (SELECT TOP 1 pp2.Capital
                     FROM CREDITO.PlanPago AS pp2
                     WHERE pp2.MovimientoCajaId = @MovimientoCajaId AND pp2.Estado = 'PAG'
                     ORDER BY pp2.Numero) AS SaldoAnterior,
                    MAX(pp.Numero) AS CuotasPagadasNum,
                    SUM(pp.Amortizacion) AS PagoDeuda,
                    SUM(pp.Interes + pp.GastosAdm) AS Interes,
                    SUM(pp.ImporteMora + pp.Cargo) AS MoraCargo,
                    SUM(pp.Descuento) AS Descuento,
                    SUM(pp.PagoLibre) AS ImporteLibreSum,
                    SUM(pp.PagoCuota) AS PagoCuota,
                    MIN(pp.CreditoId) AS CreditoId
                FROM CREDITO.PlanPago AS pp
                WHERE pp.MovimientoCajaId = @MovimientoCajaId AND pp.Estado = 'PAG';
                """,
                new { header.MovimientoCajaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var creditoId = agg.CreditoId ?? header.CreditoId
            ?? throw new InvalidOperationException("Movimiento CUO sin crédito asociado.");

        var creditoTotal = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                "SELECT ISNULL(SUM(Cuota), 0) FROM CREDITO.PlanPago WHERE CreditoId = @CreditoId;",
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var importePagadoCaja = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                """
                SELECT ISNULL(SUM(ImportePago), 0)
                FROM CREDITO.MovimientoCaja
                WHERE CreditoId = @CreditoId AND Estado = CAST(1 AS bit) AND Operacion = 'CUO'
                  AND MovimientoCajaId <= @MovimientoCajaId;
                """,
                new { CreditoId = creditoId, header.MovimientoCajaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var saldoCapital = Math.Max(0, creditoTotal - importePagadoCaja);
        var proximaCuota = await connection.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(
                """
                SELECT TOP 1 FechaVencimiento
                FROM CREDITO.PlanPago
                WHERE CreditoId = @CreditoId AND Estado = 'PEN'
                ORDER BY Numero;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var fechaOp = await fechaOperativa.ObtenerFechaAsync(cancellationToken).ConfigureAwait(false);
        var cuotasAtrazadas = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)
                FROM CREDITO.PlanPago
                WHERE CreditoId = @CreditoId AND Estado = 'PEN'
                  AND FechaVencimiento < @FechaOperativa;
                """,
                new { CreditoId = creditoId, FechaOperativa = fechaOp.Date },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var mora = await SumarMoraEnCuotasAsync(creditoId, cancellationToken).ConfigureAwait(false);
        var estadoCredito = MapEstadoCredito(header.EstadoCreditoRaw);
        var pagocuota = agg.PagoCuota ?? 0;
        var importeLibre = header.ImportePago - pagocuota;

        var productoCuota = string.IsNullOrWhiteSpace(header.Producto)
            ? $"CREDITO {creditoId}"
            : $"{header.Producto.Trim()} CREDITO {creditoId}";

        return new MovimientoCajaTicketDto(
            header.MovimientoCajaId,
            MovimientoCajaTicketLayout.CuotaCredito,
            header.PersonaId ?? 0,
            header.Cliente,
            header.User,
            header.FechaReg,
            header.Oficina,
            productoCuota,
            "PAGO CUOTA",
            string.Empty,
            header.ImportePago,
            creditoId,
            agg.SaldoAnterior,
            agg.PagoDeuda,
            agg.Interes,
            agg.MoraCargo,
            -(agg.Descuento ?? 0),
            -(agg.ImporteLibreSum ?? 0),
            importeLibre,
            header.ImportePago,
            saldoCapital,
            $"{agg.CuotasPagadasNum} de {header.NumeroCuotas}",
            proximaCuota?.ToShortDateString(),
            cuotasAtrazadas,
            mora,
            estadoCredito,
            creditoTotal,
            MedioPago: MedioLabel(header));
    }

    private async Task<MovimientoCajaTicketDto> BuildCuotaLibreAsync(
        SqlConnection connection,
        TicketHeaderRow header,
        CancellationToken cancellationToken)
    {
        var creditoId = header.CreditoId
            ?? throw new InvalidOperationException("Movimiento CUO libre sin crédito.");

        var cuotasPagadas = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT ISNULL(MAX(Numero), 0)
                FROM CREDITO.PlanPago
                WHERE CreditoId = @CreditoId AND Estado = 'PAG';
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var creditoTotal = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                "SELECT ISNULL(SUM(Cuota), 0) FROM CREDITO.PlanPago WHERE CreditoId = @CreditoId;",
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var importePagadoCaja = await connection.ExecuteScalarAsync<decimal>(
            new CommandDefinition(
                """
                SELECT ISNULL(SUM(ImportePago), 0)
                FROM CREDITO.MovimientoCaja
                WHERE CreditoId = @CreditoId AND Estado = CAST(1 AS bit) AND Operacion = 'CUO'
                  AND MovimientoCajaId <= @MovimientoCajaId;
                """,
                new { CreditoId = creditoId, header.MovimientoCajaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var saldoCapital = Math.Max(0, creditoTotal - importePagadoCaja);
        var proximaCuota = await connection.QueryFirstOrDefaultAsync<DateTime?>(
            new CommandDefinition(
                """
                SELECT TOP 1 FechaVencimiento
                FROM CREDITO.PlanPago
                WHERE CreditoId = @CreditoId AND Estado = 'PEN'
                ORDER BY Numero;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var fechaOp = await fechaOperativa.ObtenerFechaAsync(cancellationToken).ConfigureAwait(false);
        var cuotasAtrazadas = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)
                FROM CREDITO.PlanPago
                WHERE CreditoId = @CreditoId AND Estado = 'PEN'
                  AND FechaVencimiento < @FechaOperativa;
                """,
                new { CreditoId = creditoId, FechaOperativa = fechaOp.Date },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var mora = await SumarMoraEnCuotasAsync(creditoId, cancellationToken).ConfigureAwait(false);

        var productoLibre = string.IsNullOrWhiteSpace(header.Producto)
            ? $"CREDITO {creditoId}"
            : $"{header.Producto.Trim()} CREDITO {creditoId}";

        return new MovimientoCajaTicketDto(
            header.MovimientoCajaId,
            MovimientoCajaTicketLayout.CuotaLibre,
            header.PersonaId ?? 0,
            header.Cliente,
            header.User,
            header.FechaReg,
            header.Oficina,
            productoLibre,
            "PAGO LIBRE",
            header.Descripcion ?? string.Empty,
            header.ImportePago,
            creditoId,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            header.ImportePago,
            saldoCapital,
            $"{cuotasPagadas} de {header.NumeroCuotas}",
            proximaCuota?.ToShortDateString(),
            cuotasAtrazadas,
            mora,
            MapEstadoCredito(header.EstadoCreditoRaw),
            creditoTotal,
            MedioPago: MedioLabel(header));
    }

    private static MovimientoCajaTicketDto BuildInicial(TicketHeaderRow header)
    {
        var concepto = header.Operacion switch
        {
            "INI" => "PAGO INICIAL",
            "GAD" => "GASTO ADM ADELANTADO",
            "CDN" => "PAGO POR CONDONACION\n--CREDITO CANCELADO--",
            _ => header.Operacion,
        };

        var nroCredito = header.CreditoId is null or 0 ? string.Empty : $" CREDITO {header.CreditoId}";
        var articulo = string.Empty;
        if (header.IndCondonacion && header.MontoCondonacion is > 0)
        {
            var total = Math.Round(header.ImportePago + header.MontoCondonacion.Value, 2);
            articulo =
                $"MONTO TOTAL POR PAGAR   {total:N2}\n MONTO CONDONADO   {header.MontoCondonacion.Value:N2}";
        }

        return new MovimientoCajaTicketDto(
            header.MovimientoCajaId,
            MovimientoCajaTicketLayout.Simple,
            header.PersonaId ?? 0,
            header.Cliente,
            header.User,
            header.FechaReg,
            header.Oficina,
            header.Producto + nroCredito,
            concepto,
            articulo,
            header.ImportePago,
            header.CreditoId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            MedioPago: MedioLabel(header));
    }

    private async Task<MovimientoCajaTicketDto> BuildContadoAsync(
        SqlConnection connection,
        TicketHeaderRow header,
        CancellationToken cancellationToken)
    {
        var articulo = string.Empty;
        if (header.OrdenVentaId is > 0)
        {
            articulo = await connection.ExecuteScalarAsync<string>(
                new CommandDefinition(
                    """
                    SELECT STRING_AGG(ovd.Descripcion, CHAR(10)) WITHIN GROUP (ORDER BY ovd.OrdenVentaDetId)
                    FROM VENTAS.OrdenVentaDet AS ovd
                    WHERE ovd.OrdenVentaId = @OrdenVentaId;
                    """,
                    new { header.OrdenVentaId },
                    cancellationToken: cancellationToken)).ConfigureAwait(false) ?? string.Empty;
        }

        return new MovimientoCajaTicketDto(
            header.MovimientoCajaId,
            MovimientoCajaTicketLayout.Simple,
            header.PersonaId ?? 0,
            header.Cliente,
            header.User,
            header.FechaReg,
            header.Oficina,
            $"CREDIEMPRENDE HOGAR - {header.Descripcion}",
            "PAGO CONTADO",
            articulo,
            header.ImportePago,
            header.CreditoId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            MedioPago: MedioLabel(header));
    }

    private static MovimientoCajaTicketDto BuildOtros(TicketHeaderRow header)
    {
        var articulo = string.IsNullOrWhiteSpace(header.Descripcion) ? "*" : header.Descripcion;
        var concepto = header.IndEntrada ? "ENTRADA" : "SALIDA";
        var op = string.IsNullOrWhiteSpace(header.Operacion) ? string.Empty : header.Operacion.Trim();
        var producto = string.IsNullOrEmpty(op) ? "CAJA DIARIO" : $"CAJA DIARIO · {op}";

        return new MovimientoCajaTicketDto(
            header.MovimientoCajaId,
            MovimientoCajaTicketLayout.Simple,
            header.PersonaId ?? 0,
            header.Cliente,
            header.User,
            header.FechaReg,
            header.Oficina,
            producto,
            concepto,
            articulo,
            header.ImportePago,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            MedioPago: MedioLabel(header),
            Entrada: header.IndEntrada ? header.ImportePago : null,
            Salida: header.IndEntrada ? null : header.ImportePago);
    }

    private static string? MedioLabel(TicketHeaderRow header)
    {
        if (string.IsNullOrWhiteSpace(header.TipoPago))
        {
            return null;
        }

        var label = ResumenCuentaCajaParser.DisplayLabel(header.TipoPago);
        return string.IsNullOrWhiteSpace(label) ? header.TipoPago.Trim() : label;
    }

    private static string MapEstadoCredito(string? estado) =>
        estado == "PAG" ? "CANCELADO" : string.Empty;

    /// <summary>Mora operativa = suma ImporteMora de cuotas (usp_CuotasPendientes).</summary>
    private async Task<decimal> SumarMoraEnCuotasAsync(
        int creditoId,
        CancellationToken cancellationToken)
    {
        var fecha = await fechaOperativa.ObtenerFechaAsync(cancellationToken).ConfigureAwait(false);
        var rows = await cuotasPendientes
            .ListarAsync(creditoId, fecha.Date, indCancelacion: false, cancellationToken)
            .ConfigureAwait(false);
        return rows.Sum(r => r.ImporteMora ?? 0m);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class TicketHeaderRow
    {
        public int MovimientoCajaId { get; init; }
        public string Operacion { get; init; } = string.Empty;
        public int? CreditoId { get; init; }
        public int? PersonaId { get; init; }
        public decimal ImportePago { get; init; }
        public string? Descripcion { get; init; }
        public bool IndEntrada { get; init; }
        public DateTime FechaReg { get; init; }
        public string Cliente { get; init; } = string.Empty;
        public string User { get; init; } = string.Empty;
        public string Oficina { get; init; } = string.Empty;
        public string Producto { get; init; } = string.Empty;
        public short TipoPagoId { get; init; }
        public string TipoPago { get; init; } = string.Empty;
        public int? NumeroCuotas { get; init; }
        public string? EstadoCreditoRaw { get; init; }
        public bool IndCondonacion { get; init; }
        public decimal? MontoCondonacion { get; init; }
        public int? OrdenVentaId { get; init; }
    }

    private sealed class CuotaCreditoAggRow
    {
        public decimal? SaldoAnterior { get; init; }
        public int? CuotasPagadasNum { get; init; }
        public decimal? PagoDeuda { get; init; }
        public decimal? Interes { get; init; }
        public decimal? MoraCargo { get; init; }
        public decimal? Descuento { get; init; }
        public decimal? ImporteLibreSum { get; init; }
        public decimal? PagoCuota { get; init; }
        public int? CreditoId { get; init; }
    }
}
