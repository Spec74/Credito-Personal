using Credito.Modern.Application.Maestros;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Maestros;

public sealed class MarcaWriteService(IOptions<SqlDatabaseOptions> options) : IMarcaWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(GuardarMarcaRequest request, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        if (request.MarcaId < 1)
        {
            var id = await c.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "INSERT INTO ALMACEN.Marca (Denominacion, Estado) VALUES (@Denominacion, @Estado); SELECT CAST(SCOPE_IDENTITY() AS int);",
                    new { request.Denominacion, request.Estado },
                    cancellationToken: ct)).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, id, null);
        }

        var n = await c.ExecuteAsync(
            new CommandDefinition(
                "UPDATE ALMACEN.Marca SET Denominacion = @Denominacion, Estado = @Estado WHERE MarcaId = @MarcaId;",
                new { request.MarcaId, request.Denominacion, request.Estado },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, request.MarcaId, null)
            : new MaestroOperacionResponse(false, null, "Marca no encontrada.");
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(int marcaId, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.Marca
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                WHERE MarcaId = @MarcaId;
                """,
                new { MarcaId = marcaId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, marcaId, null)
            : new MaestroOperacionResponse(false, null, "Marca no encontrada.");
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}

public sealed class ModeloWriteService(IOptions<SqlDatabaseOptions> options) : IModeloWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(GuardarModeloRequest request, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        if (request.ModeloId < 1)
        {
            var id = await c.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO ALMACEN.Modelo (MarcaId, Denominacion, Estado)
                    VALUES (@MarcaId, @Denominacion, @Estado);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new { request.MarcaId, request.Denominacion, request.Estado },
                    cancellationToken: ct)).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, id, null);
        }

        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.Modelo
                SET MarcaId = @MarcaId, Denominacion = @Denominacion, Estado = @Estado
                WHERE ModeloId = @ModeloId;
                """,
                request,
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, request.ModeloId, null)
            : new MaestroOperacionResponse(false, null, "Modelo no encontrado.");
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(int modeloId, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.Modelo
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                WHERE ModeloId = @ModeloId;
                """,
                new { ModeloId = modeloId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, modeloId, null)
            : new MaestroOperacionResponse(false, null, "Modelo no encontrado.");
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}

public sealed class TipoArticuloWriteService(IOptions<SqlDatabaseOptions> options) : ITipoArticuloWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(GuardarTipoArticuloRequest request, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        if (request.TipoArticuloId < 1)
        {
            var id = await c.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO ALMACEN.TipoArticulo (Denominacion, Descripcion, Estado)
                    VALUES (@Denominacion, @Descripcion, @Estado);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new { request.Denominacion, request.Descripcion, request.Estado },
                    cancellationToken: ct)).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, id, null);
        }

        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.TipoArticulo
                SET Denominacion = @Denominacion, Descripcion = @Descripcion, Estado = @Estado
                WHERE TipoArticuloId = @TipoArticuloId;
                """,
                request,
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, request.TipoArticuloId, null)
            : new MaestroOperacionResponse(false, null, "Tipo no encontrado.");
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(int tipoArticuloId, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.TipoArticulo
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                WHERE TipoArticuloId = @TipoArticuloId;
                """,
                new { TipoArticuloId = tipoArticuloId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, tipoArticuloId, null)
            : new MaestroOperacionResponse(false, null, "Tipo no encontrado.");
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}

public sealed class OficinaWriteService(IOptions<SqlDatabaseOptions> options) : IOficinaWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(
        GuardarOficinaRequest request,
        int operadorUsuarioId,
        CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            // Responsable: elegido → en alta el operador JWT → NULL (si la columna lo permite).
            // Nunca hardcodear UsuarioId=3 (rompe bases semilla / sin ese usuario).
            int? usuarioAsignadoId;
            if (request.UsuarioAsignadoId is > 0)
            {
                usuarioAsignadoId = request.UsuarioAsignadoId;
            }
            else if (request.OficinaId < 1)
            {
                // Alta: si no eligió, queda el admin que está creando la sede.
                usuarioAsignadoId = operadorUsuarioId > 0 ? operadorUsuarioId : null;
            }
            else
            {
                // Edición con responsable limpio.
                usuarioAsignadoId = null;
            }

            if (usuarioAsignadoId is > 0)
            {
                var existe = await c.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        SELECT CASE WHEN EXISTS (
                            SELECT 1 FROM MAESTRO.Usuario
                            WHERE UsuarioId = @UsuarioId AND Estado = CAST(1 AS bit)
                        ) THEN 1 ELSE 0 END;
                        """,
                        new { UsuarioId = usuarioAsignadoId.Value },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
                if (existe != 1)
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return new MaestroOperacionResponse(
                        false,
                        null,
                        "El responsable no existe o está inactivo. Cree el usuario primero o elija otro.");
                }
            }
            else if (!await OficinaUsuarioAsignadoNullableAsync(c, tx, ct).ConfigureAwait(false))
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(
                    false,
                    null,
                    "Debe indicar un responsable (o inicie sesión con un usuario válido).");
            }

            if (request.OficinaId < 1)
            {
                if (request.IndPrincipal)
                {
                    await c.ExecuteAsync(
                        new CommandDefinition(
                            """
                            UPDATE MAESTRO.Oficina
                            SET IndPrincipal = CAST(0 AS bit)
                            WHERE IndPrincipal = CAST(1 AS bit);
                            """,
                            transaction: tx,
                            cancellationToken: ct)).ConfigureAwait(false);
                }

                var id = await c.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO MAESTRO.Oficina (
                            Denominacion, Descripcion, Telefono, IndPrincipal, Estado, UsuarioAsignadoId, Latitud, Longitud)
                        VALUES (
                            @Denominacion, @Descripcion, @Telefono, @IndPrincipal, @Estado, @UsuarioAsignadoId, @Latitud, @Longitud);
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        """,
                        new
                        {
                            request.Denominacion,
                            request.Descripcion,
                            request.Telefono,
                            request.IndPrincipal,
                            request.Estado,
                            UsuarioAsignadoId = usuarioAsignadoId,
                            request.Latitud,
                            request.Longitud,
                        },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);

                // Paridad OficinaBL.CrearOficina → CREDITO.Boveda (no TESORERIA).
                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO CREDITO.Boveda (
                            OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal,
                            FechaIniOperacion, IndCierre, IndTemporal)
                        VALUES (
                            @OficinaId, 0, 0, 0, 0,
                            CAST(GETDATE() AS date), CAST(0 AS bit), CAST(0 AS bit));
                        """,
                        new { OficinaId = id },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(true, id, null);
            }

            if (request.IndPrincipal)
            {
                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE MAESTRO.Oficina
                        SET IndPrincipal = CAST(0 AS bit)
                        WHERE IndPrincipal = CAST(1 AS bit)
                          AND OficinaId <> @OficinaId;
                        """,
                        new { request.OficinaId },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
            }

            var n = await c.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE MAESTRO.Oficina
                    SET Denominacion = @Denominacion,
                        Descripcion = @Descripcion,
                        Telefono = @Telefono,
                        IndPrincipal = @IndPrincipal,
                        Estado = @Estado,
                        UsuarioAsignadoId = @UsuarioAsignadoId,
                        Latitud = @Latitud,
                        Longitud = @Longitud
                    WHERE OficinaId = @OficinaId;
                    """,
                    new
                    {
                        request.OficinaId,
                        request.Denominacion,
                        request.Descripcion,
                        request.Telefono,
                        request.IndPrincipal,
                        request.Estado,
                        UsuarioAsignadoId = usuarioAsignadoId,
                        request.Latitud,
                        request.Longitud,
                    },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return n > 0
                ? new MaestroOperacionResponse(true, request.OficinaId, null)
                : new MaestroOperacionResponse(false, null, "Oficina no encontrada.");
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(int oficinaId, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE MAESTRO.Oficina
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                WHERE OficinaId = @OficinaId;
                """,
                new { OficinaId = oficinaId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, oficinaId, null)
            : new MaestroOperacionResponse(false, null, "Oficina no encontrada.");
    }

    private static async Task<bool> OficinaUsuarioAsignadoNullableAsync(
        SqlConnection c,
        SqlTransaction tx,
        CancellationToken ct)
    {
        var nullable = await c.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CAST(c.is_nullable AS bit)
                FROM sys.columns AS c
                WHERE c.object_id = OBJECT_ID(N'MAESTRO.Oficina')
                  AND c.name = N'UsuarioAsignadoId';
                """,
                transaction: tx,
                cancellationToken: ct)).ConfigureAwait(false);
        return nullable;
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}

public sealed class AlmacenWriteService(IOptions<SqlDatabaseOptions> options) : IAlmacenWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(GuardarAlmacenRequest request, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        if (request.AlmacenId < 1)
        {
            var id = await c.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO ALMACEN.Almacen (OficinaId, Denominacion, Descripcion, Estado)
                    VALUES (@OficinaId, @Denominacion, @Descripcion, @Estado);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    request,
                    cancellationToken: ct)).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, id, null);
        }

        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.Almacen
                SET OficinaId = @OficinaId, Denominacion = @Denominacion, Descripcion = @Descripcion, Estado = @Estado
                WHERE AlmacenId = @AlmacenId;
                """,
                request,
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, request.AlmacenId, null)
            : new MaestroOperacionResponse(false, null, "Almacén no encontrado.");
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(int almacenId, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ALMACEN.Almacen
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                WHERE AlmacenId = @AlmacenId;
                """,
                new { AlmacenId = almacenId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, almacenId, null)
            : new MaestroOperacionResponse(false, null, "Almacén no encontrado.");
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}

public sealed class ListaPrecioWriteService(IOptions<SqlDatabaseOptions> options) : IListaPrecioWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(GuardarListaPrecioRequest request, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        if (request.ListaPrecioId < 1)
        {
            var id = await c.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO VENTAS.ListaPrecio (
                        ArticuloId, Monto, Descuento, Puntos, PuntosCanje, Estado)
                    VALUES (
                        @ArticuloId, @Monto, @Descuento, @Puntos, @PuntosCanje, @Estado);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    request,
                    cancellationToken: ct)).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, id, null);
        }

        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE VENTAS.ListaPrecio
                SET ArticuloId = @ArticuloId, Monto = @Monto, Descuento = @Descuento,
                    Puntos = @Puntos, PuntosCanje = @PuntosCanje, Estado = @Estado
                WHERE ListaPrecioId = @ListaPrecioId;
                """,
                request,
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, request.ListaPrecioId, null)
            : new MaestroOperacionResponse(false, null, "Lista precio no encontrada.");
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(int listaPrecioId, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE VENTAS.ListaPrecio
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                WHERE ListaPrecioId = @ListaPrecioId;
                """,
                new { ListaPrecioId = listaPrecioId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, listaPrecioId, null)
            : new MaestroOperacionResponse(false, null, "Lista precio no encontrada.");
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}
