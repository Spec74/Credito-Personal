using System.Data;
using Credito.Modern.Application.Intranet;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Intranet;

public sealed class IntranetService(IOptions<SqlDatabaseOptions> options) : IIntranetService
{
    private readonly string _cs = options.Value.ConnectionString;

    public Task<IReadOnlyList<ComunicadoRowDto>> ListarComunicadosAsync(
        int oficinaId, int usuarioId, bool incluirVencidos, CancellationToken ct = default)
        => QueryList<ComunicadoRowDto>(
            "MAESTRO.usp_ComunicadoListar",
            new { OficinaId = oficinaId, UsuarioId = usuarioId, IncluirVencidos = incluirVencidos },
            ct);

    public async Task<(ComunicadoRowDto? Cab, IReadOnlyList<ComunicadoAdjuntoDto> Adjuntos)> ObtenerComunicadoAsync(
        int comunicadoId, int oficinaId, int usuarioId, CancellationToken ct = default)
    {
        await using var cn = await Open(ct).ConfigureAwait(false);
        await using var multi = await cn.QueryMultipleAsync(
            new CommandDefinition(
                "MAESTRO.usp_ComunicadoObtener",
                new { ComunicadoId = comunicadoId, OficinaId = oficinaId, UsuarioId = usuarioId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct)).ConfigureAwait(false);
        var cab = await multi.ReadFirstOrDefaultAsync<ComunicadoRowDto>().ConfigureAwait(false);
        var adj = (await multi.ReadAsync<ComunicadoAdjuntoDto>().ConfigureAwait(false)).ToList();
        return (cab, adj);
    }

    public async Task<int> GuardarComunicadoAsync(
        GuardarComunicadoRequest request, int oficinaId, int usuarioId, CancellationToken ct = default)
    {
        var row = await QuerySingle<IdRow>(
            "MAESTRO.usp_ComunicadoGuardar",
            new
            {
                request.ComunicadoId,
                request.Titulo,
                request.Cuerpo,
                request.Alcance,
                OficinaId = oficinaId,
                request.FechaIni,
                request.FechaFin,
                UsuarioId = usuarioId,
            },
            ct).ConfigureAwait(false);
        return row.ComunicadoId;
    }

    public Task BajaComunicadoAsync(int comunicadoId, int usuarioId, CancellationToken ct = default)
        => Exec("MAESTRO.usp_ComunicadoBaja", new { ComunicadoId = comunicadoId, UsuarioId = usuarioId }, ct);

    public Task MarcarLeidoAsync(int comunicadoId, int usuarioId, CancellationToken ct = default)
        => Exec("MAESTRO.usp_ComunicadoMarcarLeido", new { ComunicadoId = comunicadoId, UsuarioId = usuarioId }, ct);

    public async Task<int> SubirAdjuntoAsync(
        int comunicadoId, string nombre, string contentType, byte[] contenido, CancellationToken ct = default)
    {
        var row = await QuerySingle<AdjuntoIdRow>(
            "MAESTRO.usp_ComunicadoAdjuntoIns",
            new
            {
                ComunicadoId = comunicadoId,
                NombreOriginal = nombre,
                ContentType = contentType,
                Contenido = contenido,
            },
            ct).ConfigureAwait(false);
        return row.AdjuntoId;
    }

    public Task<ComunicadoAdjuntoContenidoDto?> DescargarAdjuntoAsync(
        int adjuntoId, int oficinaId, CancellationToken ct = default)
        => QuerySingleOrDefault<ComunicadoAdjuntoContenidoDto>(
            "MAESTRO.usp_ComunicadoAdjuntoGet",
            new { AdjuntoId = adjuntoId, OficinaId = oficinaId },
            ct);

    public Task<IReadOnlyList<AgendaEventoDto>> ListarAgendaAsync(
        int oficinaId, int usuarioId, DateTime desde, DateTime hasta, CancellationToken ct = default)
        => QueryList<AgendaEventoDto>(
            "MAESTRO.usp_AgendaListar",
            new { OficinaId = oficinaId, UsuarioId = usuarioId, Desde = desde, Hasta = hasta },
            ct);

    public async Task<int> GuardarAgendaAsync(
        GuardarAgendaRequest request, int oficinaId, int usuarioId, CancellationToken ct = default)
    {
        var row = await QuerySingle<EventoIdRow>(
            "MAESTRO.usp_AgendaGuardar",
            new
            {
                request.EventoId,
                request.Titulo,
                request.Notas,
                request.FechaIni,
                request.FechaFin,
                request.TodoElDia,
                request.RecordatorioMinutos,
                request.Alcance,
                OficinaId = oficinaId,
                UsuarioId = usuarioId,
            },
            ct).ConfigureAwait(false);
        return row.EventoId;
    }

    public Task BajaAgendaAsync(int eventoId, int usuarioId, CancellationToken ct = default)
        => Exec("MAESTRO.usp_AgendaBaja", new { EventoId = eventoId, UsuarioId = usuarioId }, ct);

    public Task DescartarAgendaAsync(int eventoId, int usuarioId, CancellationToken ct = default)
        => Exec("MAESTRO.usp_AgendaDescartar", new { EventoId = eventoId, UsuarioId = usuarioId }, ct);

    public Task<IReadOnlyList<BandejaItemDto>> BandejaAsync(
        int oficinaId, int usuarioId, CancellationToken ct = default)
        => QueryList<BandejaItemDto>(
            "MAESTRO.usp_BandejaNotificaciones",
            new { OficinaId = oficinaId, UsuarioId = usuarioId },
            ct);

    private sealed record IdRow(int ComunicadoId);
    private sealed record EventoIdRow(int EventoId);
    private sealed record AdjuntoIdRow(int AdjuntoId);

    private async Task<IReadOnlyList<T>> QueryList<T>(string proc, object param, CancellationToken ct)
    {
        await using var cn = await Open(ct).ConfigureAwait(false);
        var rows = await cn.QueryAsync<T>(Proc(proc, param, ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    private async Task<T> QuerySingle<T>(string proc, object param, CancellationToken ct)
    {
        await using var cn = await Open(ct).ConfigureAwait(false);
        return await cn.QuerySingleAsync<T>(Proc(proc, param, ct)).ConfigureAwait(false);
    }

    private async Task<T?> QuerySingleOrDefault<T>(string proc, object param, CancellationToken ct)
    {
        await using var cn = await Open(ct).ConfigureAwait(false);
        return await cn.QuerySingleOrDefaultAsync<T>(Proc(proc, param, ct)).ConfigureAwait(false);
    }

    private async Task Exec(string proc, object param, CancellationToken ct)
    {
        await using var cn = await Open(ct).ConfigureAwait(false);
        await cn.ExecuteAsync(Proc(proc, param, ct)).ConfigureAwait(false);
    }

    private static CommandDefinition Proc(string proc, object param, CancellationToken ct) =>
        new(proc, param, commandType: CommandType.StoredProcedure, cancellationToken: ct);

    private async Task<SqlConnection> Open(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_cs))
        {
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
        }

        var cn = new SqlConnection(_cs);
        await cn.OpenAsync(ct).ConfigureAwait(false);
        return cn;
    }
}
