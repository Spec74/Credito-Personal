using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class SaldosCajaDiarioReadService(IOptions<SqlDatabaseOptions> options)
    : ISaldosCajaDiarioReadService
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 200;

    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<SaldoCajaSesionPageDto> ListarCajaDiarioPorOficinaAsync(
        int oficinaId,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1)
        {
            return EmptyPage(page, pageSize);
        }

        const string from = """
            FROM CREDITO.CajaDiario AS cd
            INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioAsignadoId
            WHERE c.OficinaId = @OficinaId
              AND (@Buscar IS NULL
                   OR c.Denominacion LIKE '%' + @Buscar + '%'
                   OR u.NombreUsuario LIKE '%' + @Buscar + '%'
                   OR CAST(cd.CajaDiarioId AS nvarchar(20)) = @Buscar)
            """;

        const string select = """
            SELECT cd.CajaDiarioId AS Id,
                   c.Denominacion AS Caja,
                   u.NombreUsuario AS Usuario,
                   cd.SaldoInicial,
                   cd.SaldoFinal,
                   cd.FechaIniOperacion,
                   cd.FechaFinOperacion,
                   cd.IndCierre,
                   cd.TransBoveda
            """;

        return await QueryPageAsync(
            select,
            from,
            "ORDER BY cd.FechaIniOperacion DESC, cd.CajaDiarioId DESC",
            "cd.SaldoInicial",
            "cd.SaldoFinal",
            new { OficinaId = oficinaId },
            buscar,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Paridad <c>CajaChicaDiarioBL.LstSaldosCajaChicaDiarioJGrid</c>: el legado no filtra por
    /// oficina en esta grilla.
    /// </summary>
    public async Task<SaldoCajaSesionPageDto> ListarCajaChicaDiarioAsync(
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        const string from = """
            FROM CREDITO.CajaChicaDiario AS cc
            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cc.UsuarioId
            WHERE (@Buscar IS NULL
                   OR u.NombreUsuario LIKE '%' + @Buscar + '%'
                   OR CAST(cc.Id AS nvarchar(20)) = @Buscar)
            """;

        const string select = """
            SELECT cc.Id,
                   'CAJA CHICA' AS Caja,
                   u.NombreUsuario AS Usuario,
                   cc.SaldoInicial,
                   cc.SaldoFinal,
                   cc.FechaIniOperacion,
                   cc.FechaFinOperacion,
                   cc.IndCierre,
                   cc.TransBoveda
            """;

        return await QueryPageAsync(
            select,
            from,
            "ORDER BY cc.FechaIniOperacion DESC, cc.Id DESC",
            "cc.SaldoInicial",
            "cc.SaldoFinal",
            new { },
            buscar,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<SaldoCajaSesionPageDto> ListarCajaDiarioBovedaAsync(
        int bovedaId,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (bovedaId < 1)
        {
            return EmptyPage(page, pageSize);
        }

        const string from = """
            FROM CREDITO.CajaDiario AS cd
            INNER JOIN CREDITO.Caja AS c ON c.CajaId = cd.CajaId
            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = cd.UsuarioAsignadoId
            WHERE EXISTS (
                      SELECT 1
                      FROM CREDITO.BovedaMov AS bm
                      WHERE bm.CajaDiarioId = cd.CajaDiarioId
                        AND bm.BovedaId = @BovedaId)
              AND (@Buscar IS NULL
                   OR c.Denominacion LIKE '%' + @Buscar + '%'
                   OR u.NombreUsuario LIKE '%' + @Buscar + '%'
                   OR CAST(cd.CajaDiarioId AS nvarchar(20)) = @Buscar)
            """;

        const string select = """
            SELECT cd.CajaDiarioId AS Id,
                   c.Denominacion AS Caja,
                   u.NombreUsuario AS Usuario,
                   cd.SaldoInicial,
                   cd.SaldoFinal,
                   cd.FechaIniOperacion,
                   cd.FechaFinOperacion,
                   cd.IndCierre,
                   cd.TransBoveda
            """;

        // El legado usaba JOIN + DISTINCT sobre BovedaMov; con EXISTS no se duplican filas
        // cuando una caja tiene varios movimientos en la misma bóveda.
        return await QueryPageAsync(
            select,
            from,
            "ORDER BY cd.FechaIniOperacion DESC, cd.CajaDiarioId DESC",
            "cd.SaldoInicial",
            "cd.SaldoFinal",
            new { BovedaId = bovedaId },
            buscar,
            page,
            pageSize,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<SaldoCajaSesionPageDto> QueryPageAsync(
        string select,
        string from,
        string orderBy,
        string saldoInicialColumn,
        string saldoFinalColumn,
        object filterParameters,
        string? buscar,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureConnection();

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);
        var term = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();

        var parameters = new DynamicParameters(filterParameters);
        parameters.Add("Buscar", term);
        parameters.Add("Skip", (safePage - 1) * safePageSize);
        parameters.Add("Take", safePageSize);

        var sql = $"""
            SELECT COUNT(1) AS TotalRecords,
                   ISNULL(SUM({saldoInicialColumn}), 0) AS TotalSaldoInicial,
                   ISNULL(SUM({saldoFinalColumn}), 0) AS TotalSaldoFinal
            {from};

            {select}
            {from}
            {orderBy}
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var multi = await connection
            .QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        var totales = await multi.ReadFirstAsync<TotalesRow>().ConfigureAwait(false);
        var rows = (await multi.ReadAsync<SaldoCajaSesionRowDto>().ConfigureAwait(false)).ToList();

        var totalPages = totales.TotalRecords == 0
            ? 0
            : (int)Math.Ceiling(totales.TotalRecords / (double)safePageSize);

        return new SaldoCajaSesionPageDto(
            safePage,
            safePageSize,
            totales.TotalRecords,
            totalPages,
            totales.TotalSaldoInicial,
            totales.TotalSaldoFinal,
            rows);
    }

    private static SaldoCajaSesionPageDto EmptyPage(int page, int pageSize) =>
        new(
            page < 1 ? 1 : page,
            pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize),
            0,
            0,
            0m,
            0m,
            Array.Empty<SaldoCajaSesionRowDto>());

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class TotalesRow
    {
        public int TotalRecords { get; init; }
        public decimal TotalSaldoInicial { get; init; }
        public decimal TotalSaldoFinal { get; init; }
    }
}
