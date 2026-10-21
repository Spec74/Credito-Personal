using System.Data;
using Credito.Modern.Application.Pla;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Pla;

public sealed class PlaService(IOptions<SqlDatabaseOptions> options) : IPlaService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public Task<PlaParametroDto?> ObtenerParametroAsync(CancellationToken cancellationToken = default)
        => QuerySingleOrDefaultAsync<PlaParametroDto>(
            "CREDITO.usp_PlaObtenerParametro",
            null,
            cancellationToken);

    public async Task<PlaParametroDto> GuardarParametroAsync(
        GuardarPlaParametroRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId));
        }

        var row = await QuerySingleAsync<PlaParametroDto>(
            "CREDITO.usp_PlaGuardarParametro",
            new
            {
                request.TcVentaMesAnterior,
                request.TcPeriodo,
                request.UmbralIndividualMenorUsd,
                request.UmbralMultipleMenorUsd,
                UsuarioId = usuarioId,
            },
            cancellationToken).ConfigureAwait(false);
        return row;
    }

    public async Task<IReadOnlyList<PlaCandidatoRoDto>> ListarCandidatosRoAsync(
        int oficinaId,
        int anio,
        int mes,
        CancellationToken cancellationToken = default)
    {
        ValidarPeriodo(oficinaId, anio, mes);
        var rows = await QueryAsync<PlaCandidatoRoDto>(
            "CREDITO.usp_PlaCandidatosRo",
            new { OficinaId = oficinaId, Anio = anio, Mes = mes },
            cancellationToken).ConfigureAwait(false);
        return rows;
    }

    public async Task<IReadOnlyList<PlaInusualDto>> ListarInusualAsync(
        int oficinaId,
        int anio,
        int mes,
        CancellationToken cancellationToken = default)
    {
        ValidarPeriodo(oficinaId, anio, mes);
        var rows = await QueryAsync<PlaInusualDto>(
            "CREDITO.usp_PlaListarInusual",
            new { OficinaId = oficinaId, Anio = anio, Mes = mes },
            cancellationToken).ConfigureAwait(false);
        return rows;
    }

    public async Task<RegistrarPlaInusualResponse> RegistrarInusualAsync(
        RegistrarPlaInusualRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (request.OficinaId < 1 || request.PersonaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "oficina, persona y usuario deben ser >= 1.");
        }

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new ArgumentException("El motivo es obligatorio.", nameof(request));
        }

        return await QuerySingleAsync<RegistrarPlaInusualResponse>(
            "CREDITO.usp_PlaRegistrarInusual",
            new
            {
                request.OficinaId,
                request.PersonaId,
                request.CreditoId,
                request.Calificacion,
                request.Motivo,
                UsuarioId = usuarioId,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static void ValidarPeriodo(int oficinaId, int anio, int mes)
    {
        if (oficinaId < 1 || anio < 2000 || mes is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId), "oficina y periodo inválidos.");
        }
    }

    private async Task<List<T>> QueryAsync<T>(
        string procedure,
        object? param,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<T>(
            new CommandDefinition(
                procedure,
                param,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows.ToList();
    }

    private async Task<T?> QuerySingleOrDefaultAsync<T>(
        string procedure,
        object? param,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<T>(
            new CommandDefinition(
                procedure,
                param,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<T> QuerySingleAsync<T>(
        string procedure,
        object? param,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QuerySingleAsync<T>(
            new CommandDefinition(
                procedure,
                param,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
