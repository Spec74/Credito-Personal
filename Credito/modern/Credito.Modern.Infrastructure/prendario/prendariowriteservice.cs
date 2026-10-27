using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Prendario;
using Credito.Modern.Infrastructure.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SqlDatabaseOptions = Credito.Modern.Infrastructure.SqlDatabaseOptions;

namespace Credito.Modern.Infrastructure.Prendario;

public sealed class PrendarioWriteService(
    IOptions<SqlDatabaseOptions> options,
    IOptions<CreditoStorageOptions> storageOptions) : IPrendarioWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;
    private readonly string _storageRoot = CreditoGestionWriteService.ResolveStorageRoot(storageOptions.Value);

    public Task<DevolverPrendaResponse> DevolverAsync(
        DevolverPrendaRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default)
        => ExecSingleAsync<DevolverPrendaResponse>(
            "CREDITO.usp_DevolverPrenda",
            new { request.PrendaId, UsuarioId = usuarioId, request.OficinaId },
            request.PrendaId,
            request.OficinaId,
            usuarioId,
            cancellationToken);

    public Task<PrendarioFichaResponse> ActualizarFichaAsync(
        PrendarioFichaRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default)
        => ExecSingleAsync<PrendarioFichaResponse>(
            "CREDITO.usp_PrendarioActualizarFicha",
            new
            {
                request.PrendaId,
                request.OficinaId,
                UsuarioId = usuarioId,
                request.Observaciones,
            },
            request.PrendaId,
            request.OficinaId,
            usuarioId,
            cancellationToken);

    public async Task<PrendarioFichaResponse> SubirFotoAsync(
        int oficinaId,
        long prendaId,
        int usuarioId,
        string extension,
        Stream contenido,
        CancellationToken cancellationToken = default)
    {
        if (oficinaId < 1 || prendaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(prendaId), "oficinaId, prendaId y usuario deben ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var meta = await connection.QuerySingleOrDefaultAsync<(int CreditoId, string? FotoPath)>(
            new CommandDefinition(
                """
                SELECT p.CreditoId, p.FotoPath
                FROM CREDITO.Prenda AS p
                INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
                WHERE p.PrendaId = @PrendaId
                  AND c.OficinaId = @OficinaId
                  AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2);
                """,
                new { PrendaId = prendaId, OficinaId = oficinaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (meta.CreditoId < 1)
        {
            throw new InvalidOperationException("Bien no encontrado en la sede.");
        }

        var ext = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
        if (!ext.StartsWith('.'))
        {
            ext = "." + ext;
        }

        var relative = Path.Combine(meta.CreditoId.ToString(), $"prenda-{prendaId}{ext}");
        var full = Path.Combine(_storageRoot, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using (var fs = File.Create(full))
        {
            await contenido.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }

        var stored = relative.Replace('\\', '/');
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Prenda
                SET FotoPath = @FotoPath,
                    UsuarioModId = @UsuarioId,
                    FechaMod = dbo.ufnFecha()
                WHERE PrendaId = @PrendaId;
                """,
                new { FotoPath = stored, UsuarioId = usuarioId, PrendaId = prendaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return await connection.QuerySingleAsync<PrendarioFichaResponse>(
            new CommandDefinition(
                """
                SELECT PrendaId, CreditoId, Observaciones, FotoPath, Estado
                FROM CREDITO.Prenda
                WHERE PrendaId = @PrendaId;
                """,
                new { PrendaId = prendaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public Task<PrendarioRenovarResponse> RenovarAsync(
        PrendarioRenovarRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default)
        => ExecSingleAsync<PrendarioRenovarResponse>(
            "CREDITO.usp_PrendarioRenovar",
            new
            {
                request.CreditoId,
                request.OficinaId,
                request.CajaDiarioId,
                UsuarioId = usuarioId,
                request.Dias,
                request.TipoPagoId,
                FechaPagoTransferencia = request.FechaPagoTransferencia ?? string.Empty,
            },
            request.CreditoId,
            request.OficinaId,
            usuarioId,
            cancellationToken);

    public Task<PrendarioLiberarResponse> LiberarAsync(
        PrendarioLiberarRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default)
        => ExecSingleAsync<PrendarioLiberarResponse>(
            "CREDITO.usp_PrendarioLiberar",
            new
            {
                request.CreditoId,
                request.OficinaId,
                request.CajaDiarioId,
                UsuarioId = usuarioId,
                request.TipoPagoId,
                FechaPagoTransferencia = request.FechaPagoTransferencia ?? string.Empty,
            },
            request.CreditoId,
            request.OficinaId,
            usuarioId,
            cancellationToken);

    public Task<RematarPrendaResponse> RematarAsync(
        RematarPrendaRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default)
        => ExecSingleAsync<RematarPrendaResponse>(
            "CREDITO.usp_RematarPrenda",
            new { request.PrendaId, request.OficinaId, UsuarioId = usuarioId },
            request.PrendaId,
            request.OficinaId,
            usuarioId,
            cancellationToken);

    private async Task<T> ExecSingleAsync<T>(
        string proc,
        object args,
        long entidadId,
        int oficinaId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (entidadId < 1 || oficinaId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(entidadId), "oficinaId, entidad y usuario deben ser >= 1.");
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var row = await connection.QuerySingleAsync<T>(
                new CommandDefinition(
                    proc,
                    args,
                    transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return row;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
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
