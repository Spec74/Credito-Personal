using System.Data;
using Credito.Modern.Application.CreditoCartera;
using Credito.Modern.Infrastructure.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoCartera;

public sealed class ListarSaldoCarteraReadService(IOptions<SqlDatabaseOptions> options)
    : IListarSaldoCarteraReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<List<ListarSaldoCarteraRowDto>> ListarAsync(
        int anio,
        int mes,
        int oficinaId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (anio is < 1900 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(anio), "anio debe estar entre 1900 y 2100.");
        }

        if (mes is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(mes), "mes debe estar entre 1 y 12.");
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficinaId debe ser >= 1.");
        }

        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_ListarSaldoCartera",
            new { Anio = anio, Mes = mes, OficinaId = oficinaId, UsuarioId = usuarioId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<ListarSaldoCarteraRowDto>(command).ConfigureAwait(false))
            .ToList();
        if (rows.Count == 0)
        {
            return rows;
        }

        return await EnrichWithNamesAsync(connection, rows, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<ListarSaldoCarteraRowDto>> EnrichWithNamesAsync(
        SqlConnection connection,
        List<ListarSaldoCarteraRowDto> rows,
        CancellationToken cancellationToken)
    {
        var agenteIds = rows.Select(r => r.AgenteId).Where(id => id > 0).Distinct().ToArray();
        var oficinaIds = rows.Select(r => r.OficinaId).Where(id => id > 0).Distinct().ToArray();

        var agentes = new Dictionary<int, string>();
        if (agenteIds.Length > 0)
        {
            const string sqlAgentes = """
                SELECT u.UsuarioId,
                       COALESCE(
                           NULLIF(LTRIM(RTRIM(p.NombreCompleto)), N''),
                           NULLIF(LTRIM(RTRIM(u.NombreUsuario)), N''),
                           CONCAT(N'Usuario ', u.UsuarioId)) AS Nombre
                FROM MAESTRO.Usuario AS u
                LEFT JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
                WHERE u.UsuarioId IN @Ids;
                """;
            var agenteRows = await connection
                .QueryAsync<(int UsuarioId, string Nombre)>(
                    new CommandDefinition(sqlAgentes, new { Ids = agenteIds }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            foreach (var a in agenteRows)
            {
                agentes[a.UsuarioId] = a.Nombre;
            }
        }

        var oficinas = new Dictionary<int, string>();
        if (oficinaIds.Length > 0)
        {
            const string sqlOficinas = """
                SELECT OficinaId,
                       COALESCE(NULLIF(LTRIM(RTRIM(Denominacion)), N''), CONCAT(N'Oficina ', OficinaId)) AS Nombre
                FROM MAESTRO.Oficina
                WHERE OficinaId IN @Ids;
                """;
            var oficinaRows = await connection
                .QueryAsync<(int OficinaId, string Nombre)>(
                    new CommandDefinition(sqlOficinas, new { Ids = oficinaIds }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            foreach (var o in oficinaRows)
            {
                oficinas[o.OficinaId] = o.Nombre;
            }
        }

        return rows
            .Select(r => new ListarSaldoCarteraRowDto
            {
                AgenteId = r.AgenteId,
                AgenteNombre = agentes.TryGetValue(r.AgenteId, out var an)
                    ? an
                    : (r.AgenteId > 0 ? $"Usuario {r.AgenteId}" : null),
                OficinaId = r.OficinaId,
                OficinaNombre = oficinas.TryGetValue(r.OficinaId, out var on)
                    ? on
                    : (r.OficinaId > 0 ? $"Oficina {r.OficinaId}" : null),
                NroDesembolsos = r.NroDesembolsos,
                MontoDesembolsos = r.MontoDesembolsos,
                SaldoCartera = r.SaldoCartera,
                NroClientesSaldoCartera = r.NroClientesSaldoCartera,
                SaldoMoraCartera = r.SaldoMoraCartera,
                NroClientesSaldoMoraCartera = r.NroClientesSaldoMoraCartera,
                SaldoVencido = r.SaldoVencido,
                SaldoMorosidad = r.SaldoMorosidad,
                NroClientesNuevos = r.NroClientesNuevos,
                FechaCierre = r.FechaCierre,
            })
            .ToList();
    }
}
