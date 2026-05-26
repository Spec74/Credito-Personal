using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.UsuariosAdmin;
using Credito.Modern.Infrastructure.Auth;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.UsuariosAdmin;

public sealed class UsuarioAdminWriteService(IOptions<SqlDatabaseOptions> options) : IUsuarioAdminWriteService
{
    private const string ClaveResetLegacy = "123456";
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(
        GuardarUsuarioRequest request,
        CancellationToken ct = default)
    {
        Ensure();
        var apePat = (request.ApePaterno ?? string.Empty).Trim().ToUpperInvariant();
        var apeMat = (request.ApeMaterno ?? string.Empty).Trim().ToUpperInvariant();
        var nombre = (request.Nombre ?? string.Empty).Trim();
        var nombreUsuario = (request.NombreUsuario ?? string.Empty).Trim().ToUpperInvariant();
        var dni = (request.NumeroDocumento ?? string.Empty).Trim();
        var nombreCompleto = $"{apePat} {apeMat}, {nombre}";

        if (string.IsNullOrEmpty(dni) || string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(request.ClaveUsuario))
        {
            return new MaestroOperacionResponse(false, null, "DNI, usuario y clave son obligatorios.");
        }

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
                            request.Estado,
                        },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return new MaestroOperacionResponse(true, usuarioId, null);
            }

            var sqlUpdate = actualizarClave
                ? """
                  UPDATE MAESTRO.Usuario
                  SET PersonaId = @PersonaId,
                      NombreUsuario = @NombreUsuario,
                      ClaveUsuario = @ClaveUsuario,
                      Estado = @Estado
                  WHERE UsuarioId = @UsuarioId;
                  """
                : """
                  UPDATE MAESTRO.Usuario
                  SET PersonaId = @PersonaId,
                      NombreUsuario = @NombreUsuario,
                      Estado = @Estado
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

    public async Task<MaestroOperacionResponse> ActivarAsync(int usuarioId, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
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

    public async Task<MaestroOperacionResponse> ResetearClaveAsync(int usuarioId, CancellationToken ct = default)
    {
        Ensure();
        var hash = UsuarioPasswordHasher.CreateHash(ClaveResetLegacy);
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                "UPDATE MAESTRO.Usuario SET ClaveUsuario = @Clave WHERE UsuarioId = @UsuarioId;",
                new { UsuarioId = usuarioId, Clave = hash },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, usuarioId, null)
            : new MaestroOperacionResponse(false, null, "Usuario no encontrado.");
    }

    public async Task<MaestroOperacionResponse> AsignarOficinasAsync(
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
        int usuarioId,
        int oficinaId,
        int[] rolIds,
        CancellationToken ct = default)
    {
        Ensure();
        if (oficinaId < 1)
        {
            return new MaestroOperacionResponse(false, null, "oficinaId debe ser >= 1.");
        }

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
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

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
