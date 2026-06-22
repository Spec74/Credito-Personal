using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaCuadrePreviewReadService(
    IOptions<SqlDatabaseOptions> options,
    IBovedaEstadoDineroReadService estadoDineroRead) : IBovedaCuadrePreviewReadService
{
    private static readonly BovedaCuadreDenominacionDto[] Denominaciones =
    [
        new("b200", "Billete S/ 200", 200m, "billetes"),
        new("b100", "Billete S/ 100", 100m, "billetes"),
        new("b50", "Billete S/ 50", 50m, "billetes"),
        new("b20", "Billete S/ 20", 20m, "billetes"),
        new("b10", "Billete S/ 10", 10m, "billetes"),
        new("m5", "Moneda S/ 5", 5m, "monedas"),
        new("m2", "Moneda S/ 2", 2m, "monedas"),
        new("m1", "Moneda S/ 1", 1m, "monedas"),
        new("c50", "Moneda S/ 0.50", 0.50m, "monedas"),
        new("c20", "Moneda S/ 0.20", 0.20m, "monedas"),
        new("c10", "Moneda S/ 0.10", 0.10m, "monedas"),
    ];

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<BovedaCuadrePreviewDto?> ObtenerAsync(
        int oficinaId,
        int? bovedaId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (bovedaId is { } id && id < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaId), "bovedaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var boveda = await ObtenerBovedaAsync(connection, oficinaId, bovedaId, cancellationToken)
            .ConfigureAwait(false);
        if (boveda is null)
        {
            return null;
        }

        var estadoDinero = await estadoDineroRead
            .ObtenerAsync(oficinaId, boveda.SaldoFinal, cancellationToken)
            .ConfigureAwait(false);

        var mediosCaja = (await connection.QueryAsync<CajaMedioRow>(
            new CommandDefinition(
                """
                SELECT cd.CajaDiarioId,
                       c.Denominacion AS Caja,
                       u.NombreUsuario AS Responsable,
                       cd.FechaIniOperacion,
                       cd.FechaFinOperacion,
                       cd.IndCierre,
                       cd.TransBoveda,
                       cd.SaldoInicial,
                       cd.Entradas,
                       cd.Salidas,
                       cd.SaldoFinal,
                       ISNULL(mc.TipoPagoId, 1) AS TipoPagoId,
                       COALESCE(vt.Denominacion, CASE WHEN ISNULL(mc.TipoPagoId, 1) = 1 THEN 'EFECTIVO' ELSE 'SIN CLASIFICAR' END) AS TipoPago,
                       ISNULL(SUM(CASE
                           WHEN mc.MovimientoCajaId IS NULL THEN 0
                           WHEN mc.IndEntrada = CAST(1 AS bit) THEN mc.ImportePago
                           ELSE -mc.ImportePago
                       END), 0) AS Monto,
                       SUM(CASE
                           WHEN ISNULL(mc.TipoPagoId, 1) > 1
                            AND ISNULL(mce.IndTransferenciaVerificada, CAST(0 AS bit)) = CAST(0 AS bit)
                           THEN 1
                           ELSE 0
                       END) AS PagosNoVerificados
                FROM CREDITO.CajaDiario AS cd
                INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioAsignadoId
                LEFT JOIN CREDITO.MovimientoCaja AS mc ON mc.CajaDiarioId = cd.CajaDiarioId
                    AND mc.Estado = CAST(1 AS bit)
                LEFT JOIN CREDITO.MovimientoCajaExtension AS mce ON mce.MovimientoCajaId = mc.MovimientoCajaId
                LEFT JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 13 AND vt.ItemId = mc.TipoPagoId
                WHERE c.OficinaId = @OficinaId
                  AND cd.FechaIniOperacion >= @FechaInicio
                  AND (cd.TransBoveda = CAST(0 AS bit) OR cd.IndCierre = CAST(0 AS bit))
                GROUP BY cd.CajaDiarioId,
                         c.Denominacion,
                         u.NombreUsuario,
                         cd.FechaIniOperacion,
                         cd.FechaFinOperacion,
                         cd.IndCierre,
                         cd.TransBoveda,
                         cd.SaldoInicial,
                         cd.Entradas,
                         cd.Salidas,
                         cd.SaldoFinal,
                         ISNULL(mc.TipoPagoId, 1),
                         COALESCE(vt.Denominacion, CASE WHEN ISNULL(mc.TipoPagoId, 1) = 1 THEN 'EFECTIVO' ELSE 'SIN CLASIFICAR' END)
                ORDER BY c.Denominacion, u.NombreUsuario, cd.CajaDiarioId;
                """,
                new { OficinaId = oficinaId, FechaInicio = boveda.FechaIniOperacion.Date },
                cancellationToken: cancellationToken))).ToList();

        var mediosBovedaRows = (await connection.QueryAsync<MedioMontoRow>(
            new CommandDefinition(
                """
                SELECT x.TipoPagoId,
                       COALESCE(vt.Denominacion, CASE WHEN x.TipoPagoId = 1 THEN 'EFECTIVO' ELSE 'SIN CLASIFICAR' END) AS TipoPago,
                       SUM(x.Monto) AS Monto,
                       CAST(0 AS int) AS PagosNoVerificados
                FROM (
                    SELECT ISNULL(bc.TipoPagoId, 1) AS TipoPagoId,
                           bc.SaldoInicial AS Monto
                    FROM CREDITO.BovedaCuenta AS bc
                    WHERE bc.BovedaId = @BovedaId
                    UNION ALL
                    SELECT bm.TipoPagoId,
                           CASE WHEN bm.IndEntrada = CAST(1 AS bit) THEN bm.Importe ELSE -bm.Importe END AS Monto
                    FROM CREDITO.BovedaMov AS bm
                    WHERE bm.BovedaId = @BovedaId
                      AND bm.Estado = CAST(1 AS bit)
                ) AS x
                LEFT JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 13 AND vt.ItemId = x.TipoPagoId
                GROUP BY x.TipoPagoId,
                         COALESCE(vt.Denominacion, CASE WHEN x.TipoPagoId = 1 THEN 'EFECTIVO' ELSE 'SIN CLASIFICAR' END)
                HAVING ABS(SUM(x.Monto)) >= 0.005
                ORDER BY TipoPago;
                """,
                new { boveda.BovedaId },
                cancellationToken: cancellationToken))).ToList();

        var responsables = mediosCaja
            .GroupBy(row => row.CajaDiarioId)
            .Select(group =>
            {
                var first = group.First();
                var medios = group
                    .Where(row => Math.Abs(row.Monto) >= 0.005m)
                    .Select(row => ToMedio(row.TipoPagoId, row.TipoPago, row.Monto, row.PagosNoVerificados))
                    .ToList();
                var efectivo = Round2(medios.Where(m => m.EsEfectivo).Sum(m => m.Monto));
                var digital = Round2(medios.Where(m => m.EsDigitalOBanco).Sum(m => m.Monto));
                return new BovedaCuadreResponsableDto(
                    first.CajaDiarioId,
                    first.Caja,
                    first.Responsable,
                    first.FechaIniOperacion,
                    first.FechaFinOperacion,
                    first.IndCierre,
                    first.TransBoveda,
                    first.SaldoInicial,
                    first.Entradas,
                    first.Salidas,
                    first.SaldoFinal,
                    efectivo,
                    digital,
                    Round2(efectivo + digital),
                    medios);
            })
            .ToList();

        var mediosBoveda = mediosBovedaRows
            .Select(row => ToMedio(row.TipoPagoId, row.TipoPago, row.Monto, row.PagosNoVerificados))
            .ToList();

        var efectivoCajas = Round2(responsables.Sum(r => r.Efectivo));
        var efectivoBoveda = Round2(mediosBoveda.Where(m => m.EsEfectivo).Sum(m => m.Monto));
        if (efectivoBoveda == 0m)
        {
            efectivoBoveda = boveda.SaldoFinal;
        }

        var digitalCajas = Round2(responsables.Sum(r => r.DigitalBancos));
        var digitalBoveda = Round2(mediosBoveda.Where(m => m.EsDigitalOBanco).Sum(m => m.Monto));
        var digitalBancos = Round2(digitalCajas + digitalBoveda);
        var efectivoSistema = Round2(efectivoBoveda + efectivoCajas);
        var totalMedios = Round2(efectivoSistema + digitalBancos);
        var montoALlevar = efectivoSistema;
        var saldoPorLlevar = Round2(estadoDinero.TotalFondo - montoALlevar);
        var diferenciaSistema = Round2(totalMedios - estadoDinero.TotalFondo);

        var validaciones = new List<BovedaCuadreValidacionDto>
        {
            BuildValidacion(
                "efectivo-boveda",
                "Efectivo bóveda vs saldo final",
                boveda.SaldoFinal,
                efectivoBoveda,
                "Valida que el desglose por cuentas de bóveda explique el saldo final."),
            BuildValidacion(
                "cajas",
                "Efectivo cajas vs estado de dinero",
                estadoDinero.MontoCajas,
                efectivoCajas,
                "Cruza cajas pendientes/no transferidas contra los movimientos por medio de pago."),
            BuildValidacion(
                "total-fondo",
                "Total medios vs total fondo",
                estadoDinero.TotalFondo,
                totalMedios,
                "Diferencia global entre lo conciliado por medios y el fondo operativo."),
        };

        var pendientes = new List<string>();
        var pagosNoVerificados = responsables.Sum(r => r.Medios.Sum(m => m.PagosNoVerificados));
        if (pagosNoVerificados > 0)
        {
            pendientes.Add($"{pagosNoVerificados} pago(s) por transferencia/billetera aún no verificados.");
        }

        var cajasAbiertas = responsables.Count(r => !r.IndCierre);
        if (cajasAbiertas > 0)
        {
            pendientes.Add($"{cajasAbiertas} caja(s) diaria(s) continúan abiertas.");
        }

        if (Math.Abs(diferenciaSistema) >= 0.01m)
        {
            pendientes.Add("El total por medios no cuadra con el total fondo; revise movimientos o registre observación.");
        }

        return new BovedaCuadrePreviewDto(
            boveda,
            estadoDinero,
            new BovedaCuadreTotalesDto(
                efectivoBoveda,
                efectivoCajas,
                efectivoSistema,
                digitalBancos,
                totalMedios,
                estadoDinero.TotalFondo,
                montoALlevar,
                saldoPorLlevar,
                diferenciaSistema),
            responsables,
            mediosBoveda,
            validaciones,
            Denominaciones,
            pendientes);
    }

    private static async Task<BovedaAbiertaDto?> ObtenerBovedaAsync(
        SqlConnection connection,
        int oficinaId,
        int? bovedaId,
        CancellationToken cancellationToken)
    {
        var sql = bovedaId is { }
            ? """
              SELECT TOP 1 BovedaId, OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal,
                     FechaIniOperacion, FechaFinOperacion, IndCierre, IndTemporal
              FROM CREDITO.Boveda
              WHERE OficinaId = @OficinaId AND BovedaId = @BovedaId;
              """
            : """
              SELECT TOP 1 BovedaId, OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal,
                     FechaIniOperacion, FechaFinOperacion, IndCierre, IndTemporal
              FROM CREDITO.Boveda
              WHERE OficinaId = @OficinaId AND IndCierre = CAST(0 AS bit)
              ORDER BY IndTemporal, BovedaId;
              """;

        return await connection.QueryFirstOrDefaultAsync<BovedaAbiertaDto>(
            new CommandDefinition(
                sql,
                new { OficinaId = oficinaId, BovedaId = bovedaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static BovedaCuadreMedioDto ToMedio(
        int tipoPagoId,
        string? tipoPago,
        decimal monto,
        int pagosNoVerificados)
    {
        var nombre = string.IsNullOrWhiteSpace(tipoPago) ? "Sin clasificar" : tipoPago.Trim();
        var grupo = ResolverGrupo(tipoPagoId, nombre);
        var esEfectivo = grupo == "efectivo";
        return new BovedaCuadreMedioDto(
            tipoPagoId,
            nombre,
            grupo,
            Round2(monto),
            esEfectivo,
            !esEfectivo,
            !esEfectivo,
            pagosNoVerificados);
    }

    private static string ResolverGrupo(int tipoPagoId, string tipoPago)
    {
        if (tipoPagoId == 1)
        {
            return "efectivo";
        }

        var key = tipoPago.ToUpperInvariant();
        if (key.Contains("YAPE", StringComparison.Ordinal)) return "yape";
        if (key.Contains("PLIN", StringComparison.Ordinal)) return "plin";
        if (key.Contains("BCP", StringComparison.Ordinal) || key.Contains("CREDITO", StringComparison.Ordinal)) return "bcp";
        if (key.Contains("INTERBANK", StringComparison.Ordinal)) return "interbank";
        if (key.Contains("NACION", StringComparison.Ordinal) || key.Contains(" BN", StringComparison.Ordinal)) return "bn";
        if (key.Contains("BANCO", StringComparison.Ordinal)) return "banco";
        return "digital";
    }

    private static BovedaCuadreValidacionDto BuildValidacion(
        string codigo,
        string concepto,
        decimal sistema,
        decimal? contraste,
        string mensaje)
    {
        decimal? diferencia = contraste is null ? null : Round2(contraste.Value - sistema);
        var severidad = diferencia is null
            ? "info"
            : Math.Abs(diferencia.Value) < 0.01m
                ? "ok"
                : "alerta";
        return new BovedaCuadreValidacionDto(
            codigo,
            concepto,
            Round2(sistema),
            contraste is null ? null : Round2(contraste.Value),
            diferencia,
            severidad,
            mensaje);
    }

    private static decimal Round2(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class CajaMedioRow
    {
        public int CajaDiarioId { get; init; }
        public string Caja { get; init; } = string.Empty;
        public string? Responsable { get; init; }
        public DateTime FechaIniOperacion { get; init; }
        public DateTime? FechaFinOperacion { get; init; }
        public bool IndCierre { get; init; }
        public bool TransBoveda { get; init; }
        public decimal SaldoInicial { get; init; }
        public decimal Entradas { get; init; }
        public decimal Salidas { get; init; }
        public decimal SaldoFinal { get; init; }
        public int TipoPagoId { get; init; }
        public string TipoPago { get; init; } = string.Empty;
        public decimal Monto { get; init; }
        public int PagosNoVerificados { get; init; }
    }

    private sealed class MedioMontoRow
    {
        public int TipoPagoId { get; init; }
        public string TipoPago { get; init; } = string.Empty;
        public decimal Monto { get; init; }
        public int PagosNoVerificados { get; init; }
    }
}
