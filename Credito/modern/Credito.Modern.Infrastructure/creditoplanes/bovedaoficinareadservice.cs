using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class BovedaOficinaReadService(IOptions<SqlDatabaseOptions> options) : IBovedaOficinaReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<int?> GetOficinaIdByBovedaIdAsync(int bovedaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (bovedaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaId), "bovedaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = "SELECT OficinaId FROM CREDITO.Boveda WHERE BovedaId = @BovedaId;";
        var command = new CommandDefinition(
            sql,
            new { BovedaId = bovedaId },
            cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
    }

    public async Task<BovedaAbiertaDto?> GetCabeceraReporteAsync(
        int bovedaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (bovedaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaId), "bovedaId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT BovedaId,
                   OficinaId,
                   SaldoInicial,
                   Entradas,
                   Salidas,
                   SaldoFinal,
                   FechaIniOperacion,
                   FechaFinOperacion,
                   IndCierre,
                   IndTemporal
            FROM CREDITO.Boveda
            WHERE BovedaId = @BovedaId;
            """;
        var row = await connection
            .QueryFirstOrDefaultAsync<BovedaCabRow>(
                new CommandDefinition(sql, new { BovedaId = bovedaId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return row is null
            ? null
            : new BovedaAbiertaDto(
                row.BovedaId,
                row.OficinaId,
                row.SaldoInicial,
                row.Entradas,
                row.Salidas,
                row.SaldoFinal,
                row.FechaIniOperacion,
                row.FechaFinOperacion,
                row.IndCierre,
                row.IndTemporal);
    }

    private sealed class BovedaCabRow
    {
        public int BovedaId { get; init; }
        public int OficinaId { get; init; }
        public decimal SaldoInicial { get; init; }
        public decimal Entradas { get; init; }
        public decimal Salidas { get; init; }
        public decimal SaldoFinal { get; init; }
        public DateTime FechaIniOperacion { get; init; }
        public DateTime? FechaFinOperacion { get; init; }
        public bool IndCierre { get; init; }
        public bool IndTemporal { get; init; }
    }

    public async Task<bool> ExisteBovedaTemporalAbiertaAsync(
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
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM CREDITO.Boveda
                WHERE OficinaId = @OficinaId AND IndCierre = 0 AND IndTemporal = 1
            ) THEN 1 ELSE 0 END AS bit);
            """;
        var command = new CommandDefinition(
            sql,
            new { OficinaId = oficinaId },
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BovedaDestinoTransferenciaDto>> ListarDestinosTransferenciaAsync(
        int oficinaOrigenId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (oficinaOrigenId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaOrigenId), "oficinaOrigenId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        // Central fondea sucursales; sucursal remesa solo a Central (no peer-to-peer).
        const string sql = """
            DECLARE @OrigenEsCentral bit = (
                SELECT CAST(ISNULL(IndPrincipal, 0) AS bit)
                FROM MAESTRO.Oficina
                WHERE OficinaId = @OficinaOrigenId);

            SELECT
                b.BovedaId,
                b.OficinaId,
                COALESCE(o.Denominacion, CONCAT('Oficina ', b.OficinaId)) AS Oficina,
                b.SaldoFinal,
                b.FechaIniOperacion,
                CAST(ISNULL(o.IndPrincipal, 0) AS bit) AS IndPrincipal
            FROM CREDITO.Boveda AS b
            INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
            WHERE b.IndCierre = CAST(0 AS bit)
              AND b.IndTemporal = CAST(0 AS bit)
              AND b.OficinaId <> @OficinaOrigenId
              AND (
                    (@OrigenEsCentral = CAST(1 AS bit) AND ISNULL(o.IndPrincipal, 0) = CAST(0 AS bit))
                 OR (@OrigenEsCentral = CAST(0 AS bit) AND o.IndPrincipal = CAST(1 AS bit))
              )
            ORDER BY o.IndPrincipal DESC, o.Denominacion, b.BovedaId;
            """;
        var rows = await connection
            .QueryAsync<BovedaDestinoTransferenciaDto>(
                new CommandDefinition(
                    sql,
                    new { OficinaOrigenId = oficinaOrigenId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<BovedaRedPosicionDto>> ListarPosicionRedAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string oficinasSql = """
            SELECT
                o.OficinaId,
                COALESCE(o.Denominacion, CONCAT('Oficina ', o.OficinaId)) AS Oficina,
                o.IndPrincipal,
                b.BovedaId,
                ISNULL(b.SaldoInicial, 0) AS SaldoInicial,
                ISNULL(b.Entradas, 0) AS Entradas,
                ISNULL(b.Salidas, 0) AS Salidas,
                ISNULL(b.SaldoFinal, 0) AS SaldoFinal
            FROM MAESTRO.Oficina AS o
            LEFT JOIN CREDITO.Boveda AS b
                ON b.OficinaId = o.OficinaId
               AND b.IndCierre = CAST(0 AS bit)
               AND b.IndTemporal = CAST(0 AS bit)
            WHERE o.Estado = CAST(1 AS bit)
            ORDER BY o.IndPrincipal DESC, o.Denominacion, o.OficinaId;
            """;

        var oficinas = (await connection
            .QueryAsync<RedOficinaRow>(
                new CommandDefinition(oficinasSql, cancellationToken: cancellationToken))
            .ConfigureAwait(false)).ToList();

        var cajas = (await connection
            .QueryAsync<(int OficinaId, decimal Monto)>(
                new CommandDefinition(
                    """
                    SELECT c.OficinaId, ISNULL(SUM(cd.SaldoFinal), 0) AS Monto
                    FROM CREDITO.CajaDiario AS cd
                    INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                    WHERE cd.IndCierre = CAST(0 AS bit)
                      AND cd.TransBoveda = CAST(0 AS bit)
                    GROUP BY c.OficinaId;
                    """,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false))
            .ToDictionary(x => x.OficinaId, x => x.Monto);

        var chicas = (await connection
            .QueryAsync<(int OficinaId, decimal Monto)>(
                new CommandDefinition(
                    """
                    SELECT c.OficinaId, ISNULL(SUM(ccd.SaldoFinal), 0) AS Monto
                    FROM CREDITO.CajaChicaDiario AS ccd
                    INNER JOIN CREDITO.Caja AS c ON c.CajaId = ccd.Id
                    WHERE ccd.IndCierre = CAST(0 AS bit)
                      AND ccd.TransBoveda = CAST(0 AS bit)
                    GROUP BY c.OficinaId;
                    """,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false))
            .ToDictionary(x => x.OficinaId, x => x.Monto);

        var result = new List<BovedaRedPosicionDto>(oficinas.Count);
        foreach (var row in oficinas)
        {
            var sumaMedios = 0m;
            if (row.BovedaId is > 0)
            {
                sumaMedios = await connection.ExecuteScalarAsync<decimal>(
                    new CommandDefinition(
                        BovedaCuadreSql.SumaMedios,
                        new { BovedaId = row.BovedaId.Value },
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            var montoCajas = cajas.GetValueOrDefault(row.OficinaId);
            var montoChica = chicas.GetValueOrDefault(row.OficinaId);
            var delta = decimal.Round(sumaMedios - row.SaldoFinal, 2);
            result.Add(new BovedaRedPosicionDto(
                row.OficinaId,
                row.Oficina,
                row.IndPrincipal,
                row.BovedaId,
                row.SaldoInicial,
                row.Entradas,
                row.Salidas,
                row.SaldoFinal,
                sumaMedios,
                row.BovedaId is > 0 ? delta : 0m,
                montoCajas,
                montoChica,
                row.SaldoFinal + montoCajas + montoChica));
        }

        return result;
    }

    public async Task<BovedaMediosRedDto> ListarMediosRedAsync(
        int oficinaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sesion = await connection.QueryFirstOrDefaultAsync<SesionOficinaRow>(
            new CommandDefinition(
                """
                SELECT
                    o.OficinaId,
                    COALESCE(o.Denominacion, CONCAT('Oficina ', o.OficinaId)) AS Oficina,
                    o.IndPrincipal
                FROM MAESTRO.Oficina AS o
                WHERE o.OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (sesion is null)
        {
            return new BovedaMediosRedDto(false, oficinaId, $"Oficina {oficinaId}", []);
        }

        var filas = (await connection
            .QueryAsync<MedioRedRow>(
                new CommandDefinition(
                    """
                    /* Posición por medio = bóveda abierta + caja aún no transferida (TransBoveda=0).
                       Así un cobro Yape René/Plin/BCP se ve en Medios antes del cierre/TRE.
                       Tras usp_CerrarCajasDiarios (TransBoveda=1) solo cuenta BovedaMov (sin doble). */
                    ;WITH Partes AS (
                        SELECT
                            b.OficinaId,
                            bm.TipoPagoId,
                            IIF(bm.IndEntrada = 1, bm.Importe, -bm.Importe) AS Importe
                        FROM CREDITO.BovedaMov AS bm
                        INNER JOIN CREDITO.Boveda AS b ON b.BovedaId = bm.BovedaId
                        WHERE b.IndCierre = CAST(0 AS bit)
                          AND b.IndTemporal = CAST(0 AS bit)
                          AND bm.Estado = CAST(1 AS bit)
                          AND bm.TipoPagoId > 0
                          AND bm.TipoPagoId NOT BETWEEN 6 AND 8

                        UNION ALL

                        SELECT
                            c.OficinaId,
                            mc.TipoPagoId,
                            IIF(mc.IndEntrada = 1, mc.ImportePago, -mc.ImportePago) AS Importe
                        FROM CREDITO.MovimientoCaja AS mc
                        INNER JOIN CREDITO.CajaDiario AS cd ON cd.CajaDiarioId = mc.CajaDiarioId
                        INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                        WHERE cd.TransBoveda = CAST(0 AS bit)
                          AND mc.Estado = CAST(1 AS bit)
                          AND mc.TipoPagoId > 0
                          AND mc.TipoPagoId NOT BETWEEN 6 AND 8

                        UNION ALL

                        /* SaldoInicial de caja pendiente → efectivo (paridad cierre TRE). */
                        SELECT
                            c.OficinaId,
                            CAST(1 AS smallint) AS TipoPagoId,
                            cd.SaldoInicial AS Importe
                        FROM CREDITO.CajaDiario AS cd
                        INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
                        WHERE cd.TransBoveda = CAST(0 AS bit)
                          AND cd.SaldoInicial <> 0
                    ),
                    Movs AS (
                        SELECT
                            OficinaId,
                            TipoPagoId,
                            SUM(Importe) AS Importe
                        FROM Partes
                        GROUP BY OficinaId, TipoPagoId
                    )
                    SELECT
                        o.OficinaId,
                        COALESCE(o.Denominacion, CONCAT('Oficina ', o.OficinaId)) AS Oficina,
                        CAST(o.IndPrincipal AS bit) AS IndPrincipal,
                        b.BovedaId,
                        vt.ItemId AS TipoPagoId,
                        /* SI por cuenta; si no hay BovedaCuenta, el SI de cabecera va a efectivo (paridad cuadre). */
                        CAST(ROUND(
                            CASE
                                WHEN b.BovedaId IS NULL THEN CAST(0 AS decimal(16, 2))
                                WHEN NOT EXISTS (
                                        SELECT 1 FROM CREDITO.BovedaCuenta AS x
                                        WHERE x.BovedaId = b.BovedaId)
                                     AND vt.ItemId = 1
                                    THEN ISNULL(b.SaldoInicialCab, 0)
                                ELSE ISNULL(bc.SaldoInicial, 0)
                            END
                            + ISNULL(m.Importe, 0)
                        , 2) AS decimal(16, 2)) AS Monto
                    FROM MAESTRO.Oficina AS o
                    OUTER APPLY (
                        SELECT TOP (1) bv.BovedaId, bv.SaldoInicial AS SaldoInicialCab
                        FROM CREDITO.Boveda AS bv
                        WHERE bv.OficinaId = o.OficinaId
                          AND bv.IndCierre = CAST(0 AS bit)
                          AND bv.IndTemporal = CAST(0 AS bit)
                        ORDER BY bv.BovedaId DESC
                    ) AS b
                    INNER JOIN MAESTRO.ValorTabla AS vt
                        ON vt.TablaId = 13
                       AND vt.ItemId > 0
                       AND vt.ItemId NOT BETWEEN 6 AND 8
                       AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
                    LEFT JOIN CREDITO.BovedaCuenta AS bc
                        ON bc.BovedaId = b.BovedaId AND bc.TipoPagoId = vt.ItemId
                    LEFT JOIN Movs AS m
                        ON m.OficinaId = o.OficinaId AND m.TipoPagoId = vt.ItemId
                    WHERE o.Estado = CAST(1 AS bit)
                       OR o.OficinaId = @OficinaId
                    ORDER BY vt.ItemId, o.IndPrincipal DESC, o.Denominacion;
                    """,
                    new { OficinaId = oficinaId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false)).ToList();

        var marcas = BuildMarcas(sesion, filas);
        return new BovedaMediosRedDto(sesion.IndPrincipal, sesion.OficinaId, sesion.Oficina, marcas);
    }

    private static IReadOnlyList<BovedaMedioMarcaDto> BuildMarcas(
        SesionOficinaRow sesion,
        IReadOnlyList<MedioRedRow> filas)
    {
        // Vista Central:
        // - Variantes Gerente/René: saldo de esa billetera; sin desglose por sucursal (solo viven en Central).
        // - Padre Yape/Interbank: rollup de familia (2+10+11 / 3+12+13) con red completa por sede.
        // Sucursal: una card por familia (genérico + tipados legado).
        // Σ medios (banner) = bóveda local por TipoPagoId; no suma estas cards (evita doble conteo visual).
        var idsEnCatalogo = filas.Select(f => f.TipoPagoId).ToHashSet();

        (short Id, string Marca, string Variant, short[] Familia)[] catalogo = sesion.IndPrincipal
            ?
            [
                ((short)TipoPagoRules.Efectivo, "Efectivo", "efectivo", [(short)TipoPagoRules.Efectivo]),
                ((short)TipoPagoRules.YapeGerente, "Yape Gerente", "yape", [(short)TipoPagoRules.YapeGerente]),
                ((short)TipoPagoRules.YapeRene, "Yape René", "yape", [(short)TipoPagoRules.YapeRene]),
                ((short)TipoPagoRules.Yape, "Yape", "yape",
                [
                    (short)TipoPagoRules.Yape,
                    (short)TipoPagoRules.YapeGerente,
                    (short)TipoPagoRules.YapeRene,
                ]),
                ((short)TipoPagoRules.InterbankGerente, "Interbank Gerente", "interbank", [(short)TipoPagoRules.InterbankGerente]),
                ((short)TipoPagoRules.InterbankRene, "Interbank René", "interbank", [(short)TipoPagoRules.InterbankRene]),
                ((short)TipoPagoRules.Interbank, "Interbank", "interbank",
                [
                    (short)TipoPagoRules.Interbank,
                    (short)TipoPagoRules.InterbankGerente,
                    (short)TipoPagoRules.InterbankRene,
                ]),
                (4, "BCP", "bcp", [4]),
                (5, "Banco de la Nación", "banco-nacion", [5]),
                ((short)TipoPagoRules.Plin, "Plin", "plin", [(short)TipoPagoRules.Plin]),
            ]
            :
            [
                ((short)TipoPagoRules.Efectivo, "Efectivo", "efectivo", [(short)TipoPagoRules.Efectivo]),
                // Sucursal tipa genérico; familia absorbe tipados si llegaran por error/legado.
                ((short)TipoPagoRules.Yape, "Yape", "yape",
                [
                    (short)TipoPagoRules.Yape,
                    (short)TipoPagoRules.YapeGerente,
                    (short)TipoPagoRules.YapeRene,
                ]),
                ((short)TipoPagoRules.Interbank, "Interbank", "interbank",
                [
                    (short)TipoPagoRules.Interbank,
                    (short)TipoPagoRules.InterbankGerente,
                    (short)TipoPagoRules.InterbankRene,
                ]),
                (4, "BCP", "bcp", [4]),
                (5, "Banco de la Nación", "banco-nacion", [5]),
                ((short)TipoPagoRules.Plin, "Plin", "plin", [(short)TipoPagoRules.Plin]),
            ];

        var result = new List<BovedaMedioMarcaDto>(catalogo.Length);
        foreach (var (id, marca, variant, familia) in catalogo)
        {
            if (TipoPagoRules.IsCentralWallet(id) && !idsEnCatalogo.Contains(id))
            {
                continue;
            }

            var familySet = familia.ToHashSet();
            var propias = filas.Where(f => familySet.Contains(f.TipoPagoId)).ToList();
            if (!sesion.IndPrincipal)
            {
                propias = propias.Where(f => f.OficinaId == sesion.OficinaId).ToList();
            }

            // Billeteras Gerente/René: solo total; el desglose Central/Huanta/Pichari no aporta
            // (sucursales no tipan 10–13; quedan en cero).
            if (sesion.IndPrincipal && TipoPagoRules.IsCentralWallet(id))
            {
                var totalBilletera = decimal.Round(
                    propias.Where(p => p.BovedaId is > 0).Sum(p => p.Monto),
                    2,
                    MidpointRounding.AwayFromZero);
                result.Add(new BovedaMedioMarcaDto(id, marca, variant, totalBilletera, []));
                continue;
            }

            var sedes = propias
                .Where(f => f.BovedaId is > 0 && (sesion.IndPrincipal || f.OficinaId == sesion.OficinaId))
                .GroupBy(f => f.OficinaId)
                .Select(g =>
                {
                    var first = g.First();
                    return new BovedaMedioSedeDto(
                        first.OficinaId,
                        first.Oficina,
                        first.IndPrincipal,
                        first.BovedaId,
                        decimal.Round(g.Sum(x => x.Monto), 2, MidpointRounding.AwayFromZero));
                })
                .Where(s => sesion.IndPrincipal || s.OficinaId == sesion.OficinaId)
                .ToList();

            if (sesion.IndPrincipal)
            {
                // Padres Yape/Interbank (familia) y medios 1:1: red completa Central + sucursales.
                sedes = sedes
                    .Where(s => s.BovedaId is > 0)
                    .OrderByDescending(s => s.IndPrincipal)
                    .ThenBy(s => s.Oficina, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var total = sesion.IndPrincipal
                ? decimal.Round(sedes.Sum(s => s.Monto), 2, MidpointRounding.AwayFromZero)
                : decimal.Round(propias.Sum(p => p.Monto), 2, MidpointRounding.AwayFromZero);

            result.Add(new BovedaMedioMarcaDto(id, marca, variant, total, sedes));
        }

        return result;
    }

    private sealed class SesionOficinaRow
    {
        public int OficinaId { get; init; }
        public string Oficina { get; init; } = "";
        public bool IndPrincipal { get; init; }
    }

    private sealed class MedioRedRow
    {
        public int OficinaId { get; init; }
        public string Oficina { get; init; } = "";
        public bool IndPrincipal { get; init; }
        public int? BovedaId { get; init; }
        public short TipoPagoId { get; init; }
        public decimal Monto { get; init; }
    }

    private sealed class RedOficinaRow
    {
        public int OficinaId { get; init; }
        public string Oficina { get; init; } = "";
        public bool IndPrincipal { get; init; }
        public int? BovedaId { get; init; }
        public decimal SaldoInicial { get; init; }
        public decimal Entradas { get; init; }
        public decimal Salidas { get; init; }
        public decimal SaldoFinal { get; init; }
    }

    public async Task<IReadOnlyList<BovedaTransferenciaPendienteDto>> ListarTransferenciasPendientesAsync(
        int bovedaDestinoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (bovedaDestinoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaDestinoId), "bovedaDestinoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                t.BovedaMovTempId,
                t.Importe AS Monto,
                COALESCE(o.Denominacion, CONCAT('Oficina ', b.OficinaId)) AS OficinaOrigen,
                t.Glosa AS Descripcion,
                COALESCE(p.NombreCompleto, CONCAT('Usuario ', t.UsuarioRegId)) AS UsuarioReg,
                t.FechaReg
            FROM CREDITO.BovedaMovTemp AS t
            INNER JOIN CREDITO.Boveda AS b ON b.BovedaId = t.BovedaInicioId
            LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
            LEFT JOIN MAESTRO.Usuario AS u ON u.UsuarioId = t.UsuarioRegId
            LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
            WHERE t.BovedaDestinoId = @BovedaDestinoId
            ORDER BY t.FechaReg DESC, t.BovedaMovTempId DESC;
            """;
        var rows = await connection
            .QueryAsync<BovedaTransferenciaPendienteDto>(
                new CommandDefinition(
                    sql,
                    new { BovedaDestinoId = bovedaDestinoId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<int?> GetOficinaIdByBovedaMovTempDestinoAsync(
        int bovedaMovTempId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (bovedaMovTempId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaMovTempId), "bovedaMovTempId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT d.OficinaId
            FROM CREDITO.BovedaMovTemp AS t
            INNER JOIN CREDITO.Boveda AS d ON d.BovedaId = t.BovedaDestinoId
            WHERE t.BovedaMovTempId = @BovedaMovTempId;
            """;
        return await connection
            .QueryFirstOrDefaultAsync<int?>(
                new CommandDefinition(
                    sql,
                    new { BovedaMovTempId = bovedaMovTempId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<BovedaSaldoMedioDto?> ObtenerSaldoMedioDisponibleAsync(
        int bovedaId,
        short tipoPagoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (bovedaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bovedaId), "bovedaId debe ser >= 1.");
        }

        if (tipoPagoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tipoPagoId), "tipoPagoId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        const string sql = """
            SELECT
                b.BovedaId,
                b.OficinaId,
                CAST(@TipoPagoId AS smallint) AS TipoPagoId,
                CAST(ROUND(
                    CASE
                        WHEN EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = b.BovedaId)
                        THEN ISNULL((
                            SELECT bc.SaldoInicial
                            FROM CREDITO.BovedaCuenta AS bc
                            WHERE bc.BovedaId = b.BovedaId AND bc.TipoPagoId = @TipoPagoId), 0)
                        WHEN @TipoPagoId = 1 THEN ISNULL(b.SaldoInicial, 0)
                        ELSE CAST(0 AS decimal(16, 2))
                    END
                    + ISNULL((
                        SELECT SUM(IIF(m.IndEntrada = 1, m.Importe, -m.Importe))
                        FROM CREDITO.BovedaMov AS m
                        WHERE m.BovedaId = b.BovedaId
                          AND m.Estado = 1
                          AND m.TipoPagoId = @TipoPagoId), 0)
                , 2) AS decimal(16, 2)) AS Disponible
            FROM CREDITO.Boveda AS b
            WHERE b.BovedaId = @BovedaId;
            """;
        return await connection
            .QueryFirstOrDefaultAsync<BovedaSaldoMedioDto>(
                new CommandDefinition(
                    sql,
                    new { BovedaId = bovedaId, TipoPagoId = tipoPagoId },
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }
}
