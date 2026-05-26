using Credito.Modern.Application.Clientes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.Clientes;

/// <summary>Paridad <c>ClienteController.Guardar</c>, Activar, Bloquear y CrearClienteAval.</summary>
public sealed class ClienteWriteService(IOptions<SqlDatabaseOptions> options) : IClienteWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<GuardarClienteResponse> GuardarAsync(
        GuardarClienteRequest request,
        int usuarioRegId,
        DateTime fechaRegistro,
        CancellationToken cancellationToken = default)
    {
        ValidateGuardar(request, usuarioRegId);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var ocupacionId = await ResolverOcupacionIdAsync(connection, transaction, request, cancellationToken)
                .ConfigureAwait(false);
            var conyugueId = request.EstadoCivilId is 2 or 3
                ? request.ConyuguePersonaId
                : null;
            var tope = request.TopeCredito is null or 0 ? null : request.TopeCredito;

            var personaId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    """
                    SELECT TOP (1) PersonaId
                    FROM MAESTRO.Persona
                    WHERE NumeroDocumento = @NumeroDocumento;
                    """,
                    new { NumeroDocumento = request.NumeroDocumento.Trim() },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var (tipoDocumento, nombreCompleto, apePat, apeMat, nombre) =
                BuildPersonaNombres(request);
            var sexo = request.SexoMasculino ? "M" : "F";
            var personaParams = new
            {
                Nombre = nombre,
                ApePaterno = apePat,
                ApeMaterno = apeMat,
                NombreCompleto = nombreCompleto,
                TipoDocumento = tipoDocumento,
                NumeroDocumento = request.NumeroDocumento.Trim(),
                Sexo = sexo,
                TipoPersona = request.TipoPersona,
                Email = request.Email?.Trim(),
                Celular1 = request.Celular1?.Trim(),
                FechaNacimiento = request.FechaNacimiento?.Date,
                Direccion = request.Direccion?.Trim(),
                DireccionRef = request.DireccionRef?.Trim(),
                DistritoId = request.DistritoId,
                EstadoCivilId = request.EstadoCivilId,
                TipoViviendaId = request.TipoViviendaId,
                ConyuguePersonaId = conyugueId,
                Estado = request.Activo,
            };

            if (personaId is null or < 1)
            {
                personaId = await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        """
                        INSERT INTO MAESTRO.Persona (
                            Nombre, ApePaterno, ApeMaterno, NombreCompleto, TipoDocumento, NumeroDocumento,
                            Codigo, Sexo, TipoPersona, EmailPersonal, Celular1, FechaNacimiento,
                            Direccion, DireccionRef, DistritoId, EstadoCivilId, TipoViviendaId, ConyuguePersonaId, Estado)
                        VALUES (
                            @Nombre, @ApePaterno, @ApeMaterno, @NombreCompleto, @TipoDocumento, @NumeroDocumento,
                            '', @Sexo, @TipoPersona, @Email, @Celular1, @FechaNacimiento,
                            @Direccion, @DireccionRef, @DistritoId, @EstadoCivilId, @TipoViviendaId, @ConyuguePersonaId, @Estado);
                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        """,
                        personaParams,
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE MAESTRO.Persona
                        SET Nombre = @Nombre,
                            ApePaterno = @ApePaterno,
                            ApeMaterno = @ApeMaterno,
                            NombreCompleto = @NombreCompleto,
                            TipoDocumento = @TipoDocumento,
                            NumeroDocumento = @NumeroDocumento,
                            Sexo = @Sexo,
                            TipoPersona = @TipoPersona,
                            EmailPersonal = @Email,
                            Celular1 = @Celular1,
                            FechaNacimiento = @FechaNacimiento,
                            Direccion = @Direccion,
                            DireccionRef = @DireccionRef,
                            DistritoId = @DistritoId,
                            EstadoCivilId = @EstadoCivilId,
                            TipoViviendaId = @TipoViviendaId,
                            ConyuguePersonaId = @ConyuguePersonaId,
                            Estado = @Estado
                        WHERE PersonaId = @PersonaId;
                        """,
                        new
                        {
                            PersonaId = personaId.Value,
                            personaParams.Nombre,
                            personaParams.ApePaterno,
                            personaParams.ApeMaterno,
                            personaParams.NombreCompleto,
                            personaParams.TipoDocumento,
                            personaParams.NumeroDocumento,
                            personaParams.Sexo,
                            personaParams.TipoPersona,
                            personaParams.Email,
                            personaParams.Celular1,
                            personaParams.FechaNacimiento,
                            personaParams.Direccion,
                            personaParams.DireccionRef,
                            personaParams.DistritoId,
                            personaParams.EstadoCivilId,
                            personaParams.TipoViviendaId,
                            personaParams.ConyuguePersonaId,
                            personaParams.Estado,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            var pid = personaId!.Value;
            var clienteExiste = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM MAESTRO.Cliente WHERE PersonaId = @PersonaId
                    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                    """,
                    new { PersonaId = pid },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var clienteParams = new
            {
                PersonaId = pid,
                ActividadEconId = ocupacionId,
                Calificacion = string.IsNullOrWhiteSpace(request.Calificacion)
                    ? "A"
                    : request.Calificacion.Trim(),
                FechaRegistro = fechaRegistro.Date,
                Estado = request.Activo,
                Nota = request.Nota?.Trim(),
                UsuarioRegId = usuarioRegId,
                DireccionNegocio = request.DireccionNegocio?.Trim(),
                DireccionNegocioRef = request.DireccionNegocioRef?.Trim(),
                Latitud = request.Latitud,
                Longitud = request.Longitud,
                TopeCredito = tope,
                ClasificacionRiesgoSbsId = request.ClasificacionRiesgoSbsId,
                ClasificacionRiesgoSbsObs = request.ClasificacionRiesgoSbsObs?.Trim(),
            };

            if (!clienteExiste)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO MAESTRO.Cliente (
                            ClienteId, PersonaId, ActividadEconId, Calificacion, FechaRegistro,
                            Estado, Nota, UsuarioRegId, Bloqueado,
                            DireccionNegocio, DireccionNegocioRef, Latitud, Longitud, TopeCredito,
                            ClasificacionRiesgoSBS, ClasificacionRiesgoSBSObs)
                        VALUES (
                            @PersonaId, @PersonaId, @ActividadEconId, @Calificacion, @FechaRegistro,
                            @Estado, @Nota, @UsuarioRegId, CAST(0 AS bit),
                            @DireccionNegocio, @DireccionNegocioRef, @Latitud, @Longitud, @TopeCredito,
                            @ClasificacionRiesgoSbsId, @ClasificacionRiesgoSbsObs);
                        """,
                        clienteParams,
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                var preserve = request.ClienteId > 0
                    ? await connection.QuerySingleOrDefaultAsync<ClientePreserveRow>(
                        new CommandDefinition(
                            """
                            SELECT FechaRegistro, Calificacion, UsuarioRegId, Bloqueado
                            FROM MAESTRO.Cliente
                            WHERE PersonaId = @PersonaId;
                            """,
                            new { PersonaId = pid },
                            transaction: transaction,
                            cancellationToken: cancellationToken)).ConfigureAwait(false)
                    : null;

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE MAESTRO.Cliente
                        SET ActividadEconId = @ActividadEconId,
                            Calificacion = @Calificacion,
                            FechaRegistro = @FechaRegistro,
                            Estado = @Estado,
                            Nota = @Nota,
                            UsuarioRegId = @UsuarioRegId,
                            Bloqueado = @Bloqueado,
                            DireccionNegocio = @DireccionNegocio,
                            DireccionNegocioRef = @DireccionNegocioRef,
                            Latitud = @Latitud,
                            Longitud = @Longitud,
                            TopeCredito = @TopeCredito,
                            ClasificacionRiesgoSBS = @ClasificacionRiesgoSbsId,
                            ClasificacionRiesgoSBSObs = @ClasificacionRiesgoSbsObs
                        WHERE PersonaId = @PersonaId;
                        """,
                        new
                        {
                            clienteParams.PersonaId,
                            clienteParams.ActividadEconId,
                            Calificacion = preserve?.Calificacion ?? clienteParams.Calificacion,
                            FechaRegistro = preserve?.FechaRegistro ?? clienteParams.FechaRegistro,
                            clienteParams.Estado,
                            clienteParams.Nota,
                            UsuarioRegId = preserve?.UsuarioRegId ?? clienteParams.UsuarioRegId,
                            Bloqueado = preserve?.Bloqueado ?? false,
                            clienteParams.DireccionNegocio,
                            clienteParams.DireccionNegocioRef,
                            clienteParams.Latitud,
                            clienteParams.Longitud,
                            clienteParams.TopeCredito,
                            clienteParams.ClasificacionRiesgoSbsId,
                            clienteParams.ClasificacionRiesgoSbsObs,
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new GuardarClienteResponse(pid, pid);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<CrearPersonaRapidaResponse> CrearPersonaRapidaAsync(
        CrearPersonaRapidaRequest request,
        int usuarioRegId,
        DateTime fechaRegistro,
        CancellationToken cancellationToken = default)
    {
        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId));
        }

        var dni = request.Dni.Trim();
        if (dni.Length != 8)
        {
            return new CrearPersonaRapidaResponse(false, 0, "El DNI debe tener 8 dígitos.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var existe = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM MAESTRO.Persona WHERE NumeroDocumento = @Dni
                ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """,
                new { Dni = dni },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (existe)
        {
            return new CrearPersonaRapidaResponse(false, 0, "Ya existe una persona con este DNI.");
        }

        var nombre = request.Nombre.Trim().ToUpperInvariant();
        var apePat = request.ApePaterno.Trim().ToUpperInvariant();
        var apeMat = request.ApeMaterno.Trim().ToUpperInvariant();
        var nombreCompleto = $"{apePat} {apeMat}, {nombre}";

        await using var tx = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var personaId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.Persona (
                        Nombre, ApePaterno, ApeMaterno, NombreCompleto, TipoDocumento, NumeroDocumento,
                        Codigo, Sexo, TipoPersona, Celular1, Estado)
                    VALUES (
                        @Nombre, @ApePaterno, @ApeMaterno, @NombreCompleto, 'DNI', @Dni,
                        '', 'M', 'N', @Celular, CAST(1 AS bit));
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new { Nombre = nombre, ApePaterno = apePat, ApeMaterno = apeMat, NombreCompleto = nombreCompleto, Dni = dni, Celular = request.Celular?.Trim() },
                    transaction: tx,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.Cliente (
                        ClienteId, PersonaId, ActividadEconId, Calificacion, FechaRegistro,
                        Estado, UsuarioRegId, Bloqueado)
                    VALUES (
                        @PersonaId, @PersonaId, 1, 'A', @Fecha, CAST(1 AS bit), @UsuarioId, CAST(0 AS bit));
                    """,
                    new { PersonaId = personaId, Fecha = fechaRegistro.Date, UsuarioId = usuarioRegId },
                    transaction: tx,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CrearPersonaRapidaResponse(true, personaId, $"{dni} {nombreCompleto}");
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<bool> HabilitarDepuradoAsync(int personaId, CancellationToken cancellationToken = default)
    {
        if (personaId < 1)
        {
            return false;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE MAESTRO.PersonaDepurado
                SET Estado = CAST(0 AS bit)
                WHERE PersonaId = @PersonaId AND Estado = CAST(1 AS bit);
                """,
                new { PersonaId = personaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<bool> ToggleActivoAsync(int personaId, CancellationToken cancellationToken = default)
    {
        if (personaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId));
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                UPDATE MAESTRO.Cliente
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                OUTPUT INSERTED.Estado
                WHERE PersonaId = @PersonaId;
                """,
                new { PersonaId = personaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> ToggleBloqueadoAsync(int personaId, CancellationToken cancellationToken = default)
    {
        if (personaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId));
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                UPDATE MAESTRO.Cliente
                SET Bloqueado = CASE WHEN Bloqueado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                OUTPUT INSERTED.Bloqueado
                WHERE PersonaId = @PersonaId;
                """,
                new { PersonaId = personaId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static async Task<int?> ResolverOcupacionIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        GuardarClienteRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.OcupacionOtros))
        {
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.Ocupacion (Denominacion, Estado)
                    VALUES (@Denominacion, CAST(1 AS bit));
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new { Denominacion = request.OcupacionOtros.Trim() },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        return request.OcupacionId;
    }

    private static void ValidateGuardar(GuardarClienteRequest request, int usuarioRegId)
    {
        if (usuarioRegId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegId));
        }

        if (string.IsNullOrWhiteSpace(request.NumeroDocumento))
        {
            throw new ArgumentException("numeroDocumento es obligatorio.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new ArgumentException("nombre es obligatorio.", nameof(request));
        }

        if (request.TipoPersona is not ("N" or "J"))
        {
            throw new ArgumentException("tipoPersona debe ser N o J.", nameof(request));
        }

        if (request.TipoPersona == "N"
            && (string.IsNullOrWhiteSpace(request.ApePaterno) || string.IsNullOrWhiteSpace(request.ApeMaterno)))
        {
            throw new ArgumentException("Apellidos obligatorios para persona natural.", nameof(request));
        }

        if (request.EstadoCivilId is 2 or 3 && (request.ConyuguePersonaId is null or < 1))
        {
            throw new ArgumentException("Cónyuge obligatorio para estado civil casado/conviviente.", nameof(request));
        }
    }

    private static (string TipoDocumento, string NombreCompleto, string ApePat, string ApeMat, string Nombre)
        BuildPersonaNombres(GuardarClienteRequest request)
    {
        var nombre = request.Nombre.Trim().ToUpperInvariant();
        if (request.TipoPersona == "N")
        {
            var apePat = (request.ApePaterno ?? string.Empty).Trim().ToUpperInvariant();
            var apeMat = (request.ApeMaterno ?? string.Empty).Trim().ToUpperInvariant();
            return ("DNI", $"{apePat} {apeMat}, {nombre}", apePat, apeMat, nombre);
        }

        return ("RUC", nombre, string.Empty, string.Empty, nombre);
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private sealed class ClientePreserveRow
    {
        public DateTime FechaRegistro { get; init; }
        public string Calificacion { get; init; } = string.Empty;
        public int UsuarioRegId { get; init; }
        public bool Bloqueado { get; init; }
    }
}
