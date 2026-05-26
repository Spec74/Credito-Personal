using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.Maestros;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Articulos;

public sealed class ArticuloWriteService(
    IOptions<SqlDatabaseOptions> sqlOptions,
    IOptions<ArticuloStorageOptions> storageOptions) : IArticuloWriteService
{
    private readonly string _cs = sqlOptions.Value.ConnectionString;
    private readonly string _imgRoot = ResolveImgRoot(storageOptions.Value);

    public async Task<MaestroOperacionResponse> GuardarAsync(
        GuardarArticuloRequest request,
        CancellationToken ct = default)
    {
        EnsureConnection();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if (request.ArticuloId < 1)
            {
                var articuloId = await c.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO ALMACEN.Articulo (
                            ModeloId, TipoArticuloId, CodArticulo, Denominacion, Descripcion,
                            IndPerecible, IndImportado, IndCanjeable, Estado)
                        VALUES (
                            @ModeloId, @TipoArticuloId, @CodArticulo, @Denominacion, @Descripcion,
                            @IndPerecible, @IndImportado, @IndCanjeable, @Estado);
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        """,
                        request,
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);

                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO VENTAS.ListaPrecio (ArticuloId, Monto, Descuento, Estado)
                        VALUES (@ArticuloId, @Monto, @Descuento, @Estado);
                        """,
                        new
                        {
                            ArticuloId = articuloId,
                            request.Monto,
                            request.Descuento,
                            request.Estado,
                        },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(true, articuloId, null);
            }

            var n = await c.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE ALMACEN.Articulo
                    SET ModeloId = @ModeloId,
                        TipoArticuloId = @TipoArticuloId,
                        CodArticulo = @CodArticulo,
                        Denominacion = @Denominacion,
                        Descripcion = @Descripcion,
                        IndPerecible = @IndPerecible,
                        IndImportado = @IndImportado,
                        IndCanjeable = @IndCanjeable,
                        Estado = @Estado
                    WHERE ArticuloId = @ArticuloId;
                    """,
                    request,
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);

            if (n < 1)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(false, null, "Artículo no encontrado.");
            }

            var listaPrecioId = await c.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP (1) ListaPrecioId
                    FROM VENTAS.ListaPrecio
                    WHERE ArticuloId = @ArticuloId
                    ORDER BY ListaPrecioId;
                    """,
                    new { request.ArticuloId },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);

            if (listaPrecioId is >= 1)
            {
                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE VENTAS.ListaPrecio
                        SET Monto = @Monto, Descuento = @Descuento, Estado = @Estado
                        WHERE ListaPrecioId = @ListaPrecioId;
                        """,
                        new
                        {
                            ListaPrecioId = listaPrecioId.Value,
                            request.Monto,
                            request.Descuento,
                            request.Estado,
                        },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
            }
            else
            {
                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO VENTAS.ListaPrecio (ArticuloId, Monto, Descuento, Estado)
                        VALUES (@ArticuloId, @Monto, @Descuento, @Estado);
                        """,
                        new
                        {
                            request.ArticuloId,
                            request.Monto,
                            request.Descuento,
                            request.Estado,
                        },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, request.ArticuloId, null);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<MaestroOperacionResponse> SubirImagenAsync(
        int articuloId,
        Stream fileStream,
        string originalFileName,
        CancellationToken ct = default)
    {
        EnsureConnection();
        Directory.CreateDirectory(_imgRoot);

        if (!await ArticuloExisteAsync(articuloId, ct).ConfigureAwait(false))
        {
            return new MaestroOperacionResponse(false, null, "Artículo no encontrado.");
        }

        var imagenCsv = await ObtenerImagenCsvAsync(articuloId, ct).ConfigureAwait(false) ?? string.Empty;

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(ext))
        {
            ext = ".jpg";
        }

        var nextIndex = SiguienteIndiceImagen(articuloId, imagenCsv);
        var fileName = $"{articuloId}_{nextIndex}{ext}";
        var destPath = Path.Combine(_imgRoot, fileName);

        await using (var fs = File.Create(destPath))
        {
            await fileStream.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        var nuevaCsv = string.IsNullOrEmpty(imagenCsv)
            ? fileName
            : $"{imagenCsv},{fileName}";

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        await c.ExecuteAsync(
            new CommandDefinition(
                "UPDATE ALMACEN.Articulo SET Imagen = @Imagen WHERE ArticuloId = @ArticuloId;",
                new { ArticuloId = articuloId, Imagen = nuevaCsv },
                cancellationToken: ct)).ConfigureAwait(false);

        return new MaestroOperacionResponse(true, articuloId, fileName);
    }

    public async Task<MaestroOperacionResponse> EliminarImagenAsync(
        int articuloId,
        string nombreArchivo,
        CancellationToken ct = default)
    {
        EnsureConnection();
        if (string.IsNullOrWhiteSpace(nombreArchivo)
            || nombreArchivo.Contains('/', StringComparison.Ordinal)
            || nombreArchivo.Contains('\\', StringComparison.Ordinal)
            || nombreArchivo.Contains("..", StringComparison.Ordinal))
        {
            return new MaestroOperacionResponse(false, null, "Nombre de archivo inválido.");
        }

        if (!await ArticuloExisteAsync(articuloId, ct).ConfigureAwait(false))
        {
            return new MaestroOperacionResponse(false, null, "Artículo no encontrado.");
        }

        var imagenCsv = await ObtenerImagenCsvAsync(articuloId, ct).ConfigureAwait(false) ?? string.Empty;

        var path = Path.Combine(_imgRoot, nombreArchivo);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        string nuevaCsv;
        if (string.IsNullOrEmpty(imagenCsv))
        {
            nuevaCsv = string.Empty;
        }
        else
        {
            var partes = imagenCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => !string.Equals(p, nombreArchivo, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            nuevaCsv = partes.Length == 0 ? string.Empty : string.Join(',', partes);
        }

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.Articulo
                SET Imagen = CASE WHEN @Imagen = '' THEN NULL ELSE @Imagen END
                WHERE ArticuloId = @ArticuloId;
                """,
                new { ArticuloId = articuloId, Imagen = nuevaCsv },
                cancellationToken: ct)).ConfigureAwait(false);

        return n > 0
            ? new MaestroOperacionResponse(true, articuloId, null)
            : new MaestroOperacionResponse(false, null, "Artículo no encontrado.");
    }

    internal static int SiguienteIndiceImagen(int articuloId, string? imagenCsv)
    {
        if (string.IsNullOrWhiteSpace(imagenCsv))
        {
            return 1;
        }

        var prefix = $"{articuloId}_";
        var max = 0;
        foreach (var token in imagenCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var baseName = Path.GetFileNameWithoutExtension(token);
            var suffix = baseName[prefix.Length..];
            if (int.TryParse(suffix, out var n) && n > max)
            {
                max = n;
            }
        }

        return max + 1;
    }

    private async Task<bool> ArticuloExisteAsync(int articuloId, CancellationToken ct)
    {
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SELECT 1 FROM ALMACEN.Articulo WHERE ArticuloId = @ArticuloId;",
                new { ArticuloId = articuloId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n == 1;
    }

    private async Task<string?> ObtenerImagenCsvAsync(int articuloId, CancellationToken ct)
    {
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        return await c.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                "SELECT Imagen FROM ALMACEN.Articulo WHERE ArticuloId = @ArticuloId;",
                new { ArticuloId = articuloId },
                cancellationToken: ct)).ConfigureAwait(false);
    }

    public static string ResolveImgRoot(ArticuloStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.RootPath))
        {
            return Path.GetFullPath(options.RootPath);
        }

        return Path.Combine(AppContext.BaseDirectory, "imgArticulos");
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_cs))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
