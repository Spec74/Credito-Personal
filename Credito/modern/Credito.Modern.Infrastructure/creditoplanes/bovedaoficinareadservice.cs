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
        const string sql = """
            SELECT
                b.BovedaId,
                b.OficinaId,
                COALESCE(o.Denominacion, CONCAT('Oficina ', b.OficinaId)) AS Oficina,
                b.SaldoFinal,
                b.FechaIniOperacion
            FROM CREDITO.Boveda AS b
            LEFT JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
            WHERE b.IndCierre = CAST(0 AS bit)
              AND b.IndTemporal = CAST(0 AS bit)
              AND b.OficinaId <> @OficinaOrigenId
            ORDER BY o.Denominacion, b.BovedaId;
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
                    ;WITH Movs AS (
                        SELECT
                            b.OficinaId,
                            bm.TipoPagoId,
                            SUM(IIF(bm.IndEntrada = 1, bm.Importe, -bm.Importe)) AS Importe
                        FROM CREDITO.BovedaMov AS bm
                        INNER JOIN CREDITO.Boveda AS b ON b.BovedaId = bm.BovedaId
                        WHERE b.IndCierre = CAST(0 AS bit)
                          AND b.IndTemporal = CAST(0 AS bit)
                          AND bm.Estado = CAST(1 AS bit)
                          AND bm.TipoPagoId > 0
                          AND bm.TipoPagoId NOT BETWEEN 6 AND 8
                        GROUP BY b.OficinaId, bm.TipoPagoId
                    )
                    SELECT
                        o.OficinaId,
                        COALESCE(o.Denominacion, CONCAT('Oficina ', o.OficinaId)) AS Oficina,
                        CAST(o.IndPrincipal AS bit) AS IndPrincipal,
                        b.BovedaId,
                        vt.ItemId AS TipoPagoId,
                        CAST(ISNULL(bc.SaldoInicial, 0) + ISNULL(m.Importe, 0) AS decimal(16, 2)) AS Monto
                    FROM MAESTRO.Oficina AS o
                    OUTER APPLY (
                        SELECT TOP (1) bv.BovedaId
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
        // Modelo (sin doble conteo en cards):
        // - Central tipa Gerente/René (10–13) → cards solo Central.
        // - Yape(2)/Interbank(3) son el medio de red/sucursales → mismas sedes que BCP/Plin
        //   (Central/Huanta/Pichari), siempre visibles aunque estén en 0.
        // - Sucursal: un Yape / un Interbank (familia 2+10+11 / 3+12+13) solo de su oficina.
        // Σ medios del cuadre viene de la bóveda local (no de sumar estas cards de red).
        var idsEnCatalogo = filas.Select(f => f.TipoPagoId).ToHashSet();

        (short Id, string Marca, string Variant, short[] Familia)[] catalogo = sesion.IndPrincipal
            ?
            [
                (1, "Efectivo", "efectivo", [1]),
                (10, "Yape Gerente", "yape", [10]),
                (11, "Yape René", "yape", [11]),
                (2, "Yape", "yape", [2]),
                (12, "Interbank Gerente", "interbank", [12]),
                (13, "Interbank René", "interbank", [13]),
                (3, "Interbank", "interbank", [3]),
                (4, "BCP", "bcp", [4]),
                (5, "Banco de la Nación", "banco-nacion", [5]),
                (9, "Plin", "plin", [9]),
            ]
            :
            [
                (1, "Efectivo", "efectivo", [1]),
                (2, "Yape", "yape", [2, 10, 11]),
                (3, "Interbank", "interbank", [3, 12, 13]),
                (4, "BCP", "bcp", [4]),
                (5, "Banco de la Nación", "banco-nacion", [5]),
                (9, "Plin", "plin", [9]),
            ];

        var result = new List<BovedaMedioMarcaDto>(catalogo.Length);
        foreach (var (id, marca, variant, familia) in catalogo)
        {
            if ((id is 10 or 11 or 12 or 13) && !idsEnCatalogo.Contains(id))
            {
                continue;
            }

            var familySet = familia.ToHashSet();
            var propias = filas.Where(f => familySet.Contains(f.TipoPagoId)).ToList();
            if (!sesion.IndPrincipal)
            {
                propias = propias.Where(f => f.OficinaId == sesion.OficinaId).ToList();
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
                // Billeteras persona: solo la sede Central.
                if (id is 10 or 11 or 12 or 13)
                {
                    sedes = sedes.Where(s => s.IndPrincipal).ToList();
                }

                // Yape/Interbank genéricos: todas las sedes (paridad visual con BCP/Plin).
                // No se ocultan en cero: la red debe verse completa.
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
}
