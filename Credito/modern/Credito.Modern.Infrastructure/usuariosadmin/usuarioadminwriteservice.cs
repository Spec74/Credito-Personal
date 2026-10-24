using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Application.Validation;
using Credito.Modern.Infrastructure.Auth;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.UsuariosAdmin;

public sealed class UsuarioAdminWriteService(IOptions<SqlDatabaseOptions> options) : IUsuarioAdminWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(
        UsuarioAdminScope scope,
        GuardarUsuarioRequest request,
        CancellationToken ct = default)
    {
        Ensure();
        var error = UsuarioValidacion.ValidarGuardar(request);
        if (error is not null)
        {
            return new MaestroOperacionResponse(false, null, error);
        }

        var apePat = (request.ApePaterno ?? string.Empty).Trim().ToUpperInvariant();
        var apeMat = (request.ApeMaterno ?? string.Empty).Trim().ToUpperInvariant();
        var nombre = (request.Nombre ?? string.Empty).Trim();
        var nombreUsuario = (request.NombreUsuario ?? string.Empty).Trim().ToUpperInvariant();
        var dni = (request.NumeroDocumento ?? string.Empty).Trim();
        var nombreCompleto = $"{apePat} {apeMat}, {nombre}";

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);

        var duplicado = await c.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM MAESTRO.Usuario AS u
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = u.PersonaId
                WHERE p.NumeroDocumento = @NumeroDocumento
                  AND (@UsuarioId < 1 OR u.UsuarioId <> @UsuarioId);
                """,
                new { NumeroDocumento = dni, request.UsuarioId },
                cancellationToken: ct)).ConfigureAwait(false);
        if (duplicado > 0)
        {
            return new MaestroOperacionResponse(false, null, "Ya existe un usuario con ese DNI.");
        }

        var loginDuplicado = await c.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(1)
                FROM MAESTRO.Usuario AS u
                WHERE UPPER(LTRIM(RTRIM(u.NombreUsuario))) = @NombreUsuario
                  AND u.Estado = CAST(1 AS bit)
                  AND (@UsuarioId < 1 OR u.UsuarioId <> @UsuarioId);
                """,
                new { NombreUsuario = nombreUsuario, request.UsuarioId },
                cancellationToken: ct)).ConfigureAwait(false);
        if (loginDuplicado > 0)
        {
            return new MaestroOperacionResponse(
                false,
                null,
                $"Ya existe un usuario activo con el nombre de usuario «{nombreUsuario}». Elija otro o desactive el duplicado.");
        }

        var actualizarClave = !string.IsNullOrWhiteSpace(request.ClaveUsuario)
            && !string.Equals(request.ClaveUsuario, "********", StringComparison.Ordinal);
        string? claveAlmacenar = null;
        if (actualizarClave)
        {
            claveAlmacenar = UsuarioPasswordHasher.LooksLikeStoredHash(request.ClaveUsuario)
                ? request.ClaveUsuario
                : UsuarioPasswordHasher.CreateHash(request.ClaveUsuario);
        }
        else if (request.UsuarioId < 1)
        {
            return new MaestroOperacionResponse(false, null, "La clave es obligatoria al crear usuario.");
        }

        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var personaId = await c.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    "SELECT PersonaId FROM MAESTRO.Persona WHERE NumeroDocumento = @NumeroDocumento;",
                    new { NumeroDocumento = dni },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);

            if (personaId is null or < 1)
            {
                personaId = await c.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO MAESTRO.Persona (
                            Nombre, ApePaterno, ApeMaterno, NombreCompleto, TipoDocumento, NumeroDocumento,
                            Sexo, TipoPersona, EmailPersonal, FechaNacimiento, Direccion, Estado, Celular1)
                        VALUES (
                            @Nombre, @ApePaterno, @ApeMaterno, @NombreCompleto, 'DNI', @NumeroDocumento,
                            @Sexo, 'N', @EmailPersonal, @FechaNacimiento, @Direccion, @Estado, @Celular1);
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        """,
                        new
                        {
                            Nombre = nombre,
                            ApePaterno = apePat,
                            ApeMaterno = apeMat,
                            NombreCompleto = nombreCompleto,
                            NumeroDocumento = dni,
                            Sexo = request.Sexo,
                            EmailPersonal = request.EmailPersonal,
                            FechaNacimiento = request.FechaNacimiento,
                            Direccion = request.Direccion,
                            Estado = request.Estado,
                            Celular1 = request.TelefonoMovil,
                        },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
            }
            else
            {
                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE MAESTRO.Persona
                        SET Nombre = @Nombre,
                            ApePaterno = @ApePaterno,
                            ApeMaterno = @ApeMaterno,
                            NombreCompleto = @NombreCompleto,
                            Sexo = @Sexo,
                            FechaNacimiento = @FechaNacimiento,
                            Celular1 = @Celular1,
                            EmailPersonal = @EmailPersonal,
                            Direccion = @Direccion,
                            Estado = @Estado
                        WHERE PersonaId = @PersonaId;
                        """,
                        new
                        {
                            PersonaId = personaId.Value,
                            Nombre = nombre,
                            ApePaterno = apePat,
                            ApeMaterno = apeMat,
                            NombreCompleto = nombreCompleto,
                            Sexo = request.Sexo,
                            FechaNacimiento = request.FechaNacimiento,
                            Celular1 = request.TelefonoMovil,
                            EmailPersonal = request.EmailPersonal,
                            Direccion = request.Direccion,
                            Estado = request.Estado,
                        },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
            }

            if (request.UsuarioId < 1)
            {
                var usuarioId = await c.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO MAESTRO.Usuario (PersonaId, NombreUsuario, ClaveUsuario, Estado)
                        VALUES (@PersonaId, @NombreUsuario, @ClaveUsuario, @Estado);
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        """,
                        new
                        {
                            PersonaId = personaId.Value,
                            NombreUsuario = nombreUsuario,
                            ClaveUsuario = claveAlmacenar,
                            Estado = true,
                        },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
                await AsegurarSedeAsync(c, tx, usuarioId, scope.OficinaId, activo: true, ct)
                    .ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(true, usuarioId, null);
            }

            if (!scope.EsCentral
                && !await PerteneceASedeAsync(c, tx, request.UsuarioId, scope.OficinaId, soloActivo: false, ct)
                    .ConfigureAwait(false))
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(false, null, "Este usuario no pertenece a esta sucursal.");
            }

            var sqlUpdate = actualizarClave
                ? """
                  UPDATE MAESTRO.Usuario
                  SET PersonaId = @PersonaId,
                      NombreUsuario = @NombreUsuario,
                      ClaveUsuario = @ClaveUsuario,
                      Estado = CASE WHEN @PuedeEstado = 1 THEN @Estado ELSE Estado END
                  WHERE UsuarioId = @UsuarioId;
                  """
                : """
                  UPDATE MAESTRO.Usuario
                  SET PersonaId = @PersonaId,
                      NombreUsuario = @NombreUsuario,
                      Estado = CASE WHEN @PuedeEstado = 1 THEN @Estado ELSE Estado END
                  WHERE UsuarioId = @UsuarioId;
                  """;

            var n = await c.ExecuteAsync(
                new CommandDefinition(
                    sqlUpdate,
                    new
                    {
                        request.UsuarioId,
                        PersonaId = personaId.Value,
                        NombreUsuario = nombreUsuario,
                        ClaveUsuario = claveAlmacenar,
                        request.Estado,
                        PuedeEstado = scope.EsCentral ? 1 : 0,
                    },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);
            if (n < 1)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(false, null, "Usuario no encontrado.");
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, request.UsuarioId, null);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        if (scope.EsCentral)
        {
            var n = await c.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE MAESTRO.Usuario
                    SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                    WHERE UsuarioId = @UsuarioId;
                    """,
                    new { UsuarioId = usuarioId },
                    cancellationToken: ct)).ConfigureAwait(false);
            return n > 0
                ? new MaestroOperacionResponse(true, usuarioId, null)
                : new MaestroOperacionResponse(false, null, "Usuario no encontrado.");
        }

        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if (!await PerteneceASedeAsync(c, tx, usuarioId, scope.OficinaId, soloActivo: false, ct)
                    .ConfigureAwait(false))
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(false, null, "Este usuario no pertenece a esta sucursal.");
            }

            var activo = await c.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT CAST(CASE WHEN uo.Estado = CAST(1 AS bit) THEN 1 ELSE 0 END AS bit)
                    FROM MAESTRO.UsuarioOficina AS uo
                    WHERE uo.UsuarioId = @UsuarioId AND uo.OficinaId = @OficinaId;
                    """,
                    new { UsuarioId = usuarioId, scope.OficinaId },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);

            await AsegurarSedeAsync(c, tx, usuarioId, scope.OficinaId, activo: !activo, ct)
                .ConfigureAwait(false);
            if (activo)
            {
                await QuitarRolesSedeAsync(c, tx, usuarioId, scope.OficinaId, ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, usuarioId, null);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<ResetearClaveResponse> ResetearClaveAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        CancellationToken ct = default)
    {
        Ensure();
        var temporal = UsuarioPasswordHasher.GenerateTemporaryPassword();
        var hash = UsuarioPasswordHasher.CreateTemporaryHash(temporal);
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        if (!scope.EsCentral
            && !await PerteneceASedeAsync(c, null, usuarioId, scope.OficinaId, soloActivo: true, ct)
                .ConfigureAwait(false))
        {
            return new ResetearClaveResponse(false, null, "Este usuario no pertenece a esta sucursal.", null);
        }

        var n = await c.ExecuteAsync(
            new CommandDefinition(
                "UPDATE MAESTRO.Usuario SET ClaveUsuario = @Clave WHERE UsuarioId = @UsuarioId;",
                new { UsuarioId = usuarioId, Clave = hash },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new ResetearClaveResponse(
                true,
                usuarioId,
                "Clave temporal generada. El usuario deberá cambiarla al iniciar sesión.",
                temporal)
            : new ResetearClaveResponse(false, null, "Usuario no encontrado.", null);
    }

    public async Task<MaestroOperacionResponse> CambiarClaveAsync(
        int usuarioId,
        string claveActual,
        string claveNueva,
        CancellationToken ct = default)
    {
        Ensure();
        var claveErr = UsuarioValidacion.ValidarClaveNueva(claveNueva);
        if (claveErr is not null)
        {
            return new MaestroOperacionResponse(false, null, claveErr);
        }

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var stored = await c.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                "SELECT ClaveUsuario FROM MAESTRO.Usuario WHERE UsuarioId = @UsuarioId AND Estado = CAST(1 AS bit);",
                new { UsuarioId = usuarioId },
                cancellationToken: ct)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(stored))
        {
            return new MaestroOperacionResponse(false, null, "Usuario no encontrado.");
        }

        if (!UsuarioPasswordHasher.Verify(stored, claveActual ?? string.Empty, out _))
        {
            return new MaestroOperacionResponse(false, null, "La clave actual no es correcta.");
        }

        if (string.Equals(claveActual?.Trim(), claveNueva.Trim(), StringComparison.Ordinal))
        {
            return new MaestroOperacionResponse(false, null, "La clave nueva debe ser distinta a la actual.");
        }

        var hash = UsuarioPasswordHasher.CreateHash(claveNueva.Trim());
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                "UPDATE MAESTRO.Usuario SET ClaveUsuario = @Clave WHERE UsuarioId = @UsuarioId;",
                new { UsuarioId = usuarioId, Clave = hash },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, usuarioId, null)
            : new MaestroOperacionResponse(false, null, "No se pudo actualizar la clave.");
    }

    public async Task<MaestroOperacionResponse> AsignarOficinasAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        int[] oficinaIds,
        CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if (!scope.EsCentral)
            {
                if (!await PerteneceASedeAsync(c, tx, usuarioId, scope.OficinaId, soloActivo: false, ct)
                        .ConfigureAwait(false)
                    && !oficinaIds.Contains(scope.OficinaId))
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return new MaestroOperacionResponse(false, null, "Este usuario no pertenece a esta sucursal.");
                }

                var queda = oficinaIds.Contains(scope.OficinaId);
                await AsegurarSedeAsync(c, tx, usuarioId, scope.OficinaId, queda, ct).ConfigureAwait(false);
                if (!queda)
                {
                    await QuitarRolesSedeAsync(c, tx, usuarioId, scope.OficinaId, ct).ConfigureAwait(false);
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(true, usuarioId, null);
            }

            await c.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM MAESTRO.UsuarioOficina WHERE UsuarioId = @UsuarioId;",
                    new { UsuarioId = usuarioId },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);

            foreach (var oficinaId in oficinaIds.Where(id => id >= 1).Distinct())
            {
                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
                        VALUES (@UsuarioId, @OficinaId, CAST(1 AS bit));
                        """,
                        new { UsuarioId = usuarioId, OficinaId = oficinaId },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
            }

            await c.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE ur
                    FROM MAESTRO.UsuarioRol AS ur
                    WHERE ur.UsuarioId = @UsuarioId
                      AND NOT EXISTS (
                          SELECT 1
                          FROM MAESTRO.UsuarioOficina AS uo
                          WHERE uo.UsuarioId = ur.UsuarioId
                            AND uo.OficinaId = ur.OficinaId
                            AND uo.Estado = CAST(1 AS bit));
                    """,
                    new { UsuarioId = usuarioId },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, usuarioId, null);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<MaestroOperacionResponse> AsignarRolesAsync(
        UsuarioAdminScope scope,
        int usuarioId,
        int oficinaId,
        int[] rolIds,
        CancellationToken ct = default)
    {
        Ensure();
        if (!scope.EsCentral && oficinaId != scope.OficinaId)
        {
            return new MaestroOperacionResponse(false, null, "Solo puede asignar roles de esta sucursal.");
        }

        var rolesErr = UsuarioValidacion.ValidarAsignarRoles(oficinaId, rolIds);
        if (rolesErr is not null)
        {
            return new MaestroOperacionResponse(false, null, rolesErr);
        }

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        if (!await PerteneceASedeAsync(c, null, usuarioId, oficinaId, soloActivo: true, ct)
                .ConfigureAwait(false))
        {
            return new MaestroOperacionResponse(
                false,
                null,
                "Asigne primero la oficina antes de definir roles.");
        }

        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            await c.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM MAESTRO.UsuarioRol
                    WHERE UsuarioId = @UsuarioId AND OficinaId = @OficinaId;
                    """,
                    new { UsuarioId = usuarioId, OficinaId = oficinaId },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);

            foreach (var rolId in rolIds.Where(id => id >= 1).Distinct())
            {
                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO MAESTRO.UsuarioRol (UsuarioId, OficinaId, RolId)
                        VALUES (@UsuarioId, @OficinaId, @RolId);
                        """,
                        new { UsuarioId = usuarioId, OficinaId = oficinaId, RolId = rolId },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, usuarioId, null);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<bool> PerteneceASedeAsync(
        SqlConnection c,
        SqlTransaction? tx,
        int usuarioId,
        int oficinaId,
        bool soloActivo,
        CancellationToken ct)
    {
        var sql = soloActivo
            ? """
              SELECT COUNT(1)
              FROM MAESTRO.UsuarioOficina AS uo
              WHERE uo.UsuarioId = @UsuarioId
                AND uo.OficinaId = @OficinaId
                AND uo.Estado = CAST(1 AS bit);
              """
            : """
              SELECT COUNT(1)
              FROM MAESTRO.UsuarioOficina AS uo
              WHERE uo.UsuarioId = @UsuarioId
                AND uo.OficinaId = @OficinaId;
              """;
        var n = await c.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { UsuarioId = usuarioId, OficinaId = oficinaId },
                transaction: tx,
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0;
    }

    private static async Task AsegurarSedeAsync(
        SqlConnection c,
        SqlTransaction tx,
        int usuarioId,
        int oficinaId,
        bool activo,
        CancellationToken ct)
    {
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE MAESTRO.UsuarioOficina
                SET Estado = @Estado
                WHERE UsuarioId = @UsuarioId AND OficinaId = @OficinaId;
                """,
                new
                {
                    UsuarioId = usuarioId,
                    OficinaId = oficinaId,
                    Estado = activo,
                },
                transaction: tx,
                cancellationToken: ct)).ConfigureAwait(false);
        if (n > 0)
        {
            return;
        }

        await c.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
                VALUES (@UsuarioId, @OficinaId, @Estado);
                """,
                new
                {
                    UsuarioId = usuarioId,
                    OficinaId = oficinaId,
                    Estado = activo,
                },
                transaction: tx,
                cancellationToken: ct)).ConfigureAwait(false);
    }

    private static Task QuitarRolesSedeAsync(
        SqlConnection c,
        SqlTransaction tx,
        int usuarioId,
        int oficinaId,
        CancellationToken ct) =>
        c.ExecuteAsync(
            new CommandDefinition(
                """
                DELETE FROM MAESTRO.UsuarioRol
                WHERE UsuarioId = @UsuarioId AND OficinaId = @OficinaId;
                """,
                new { UsuarioId = usuarioId, OficinaId = oficinaId },
                transaction: tx,
                cancellationToken: ct));

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
