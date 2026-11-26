using System.Data;
using Credito.Modern.Application.MediosPago;
using Credito.Modern.Infrastructure.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.MediosPago;

public sealed class OficinaMedioPagoService(IOptions<SqlDatabaseOptions> options) : IOficinaMedioPagoService
{
    private readonly string _cs =
        options.Value.ConnectionString
        ?? throw new InvalidOperationException("CreditoDatabase:ConnectionString no configurada.");

    private sealed class Row
    {
        public int OficinaMedioPagoId { get; init; }
        public int OficinaId { get; init; }
        public int? ParentOficinaMedioPagoId { get; init; }
        public short TipoPagoId { get; init; }
        public string Nombre { get; init; } = "";
        public string MarcaCodigo { get; init; } = "otro";
        public int Orden { get; init; }
        public bool Activo { get; init; }
        public bool EsDigital { get; init; }
        public bool UsaCelular { get; init; }
        public string? Telefono { get; init; }
        public bool UsaCuenta { get; init; }
        public string? NumeroCuenta { get; init; }
        public string? Cci { get; init; }
        public bool UsaQr { get; init; }
        public string? QrEmvPayload { get; init; }
        public string? QrNota { get; init; }
        public string? FamiliaCodigo { get; init; }
        public bool SoloCentral { get; init; }
    }

    public async Task<IReadOnlyList<OficinaMedioPagoDto>> ListByOficinaAsync(
        int oficinaId,
        bool incluirInactivos,
        bool soloRaices,
        CancellationToken cancellationToken)
    {
        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId));
        }

        await using var connection = new SqlConnection(_cs);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Multisucursal: cada oficina nueva recibe su catálogo base al primer acceso.
        await OficinaMedioPagoSeedSql
            .EnsureSemillaOficinaAsync(connection, transaction: null, oficinaId, cancellationToken)
            .ConfigureAwait(false);

        var rows = (
            await connection
                .QueryAsync<Row>(
                    new CommandDefinition(
                        """
                        SELECT
                            OficinaMedioPagoId, OficinaId, ParentOficinaMedioPagoId, TipoPagoId,
                            Nombre, MarcaCodigo, Orden, Activo, EsDigital,
                            UsaCelular, Telefono, UsaCuenta, NumeroCuenta, Cci,
                            UsaQr, QrEmvPayload, QrNota, FamiliaCodigo, SoloCentral
                        FROM CREDITO.OficinaMedioPago
                        WHERE OficinaId = @OficinaId
                          AND (@IncluirInactivos = 1 OR Activo = 1)
                        ORDER BY Orden, Nombre, OficinaMedioPagoId;
                        """,
                        new { OficinaId = oficinaId, IncluirInactivos = incluirInactivos },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false)
        ).ToList();

        var dtos = rows.Select(ToDto).ToList();
        var byParent = dtos
            .Where(d => d.ParentOficinaMedioPagoId is > 0)
            .GroupBy(d => d.ParentOficinaMedioPagoId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<OficinaMedioPagoDto>)g.ToList());

        var roots = dtos
            .Where(d => d.ParentOficinaMedioPagoId is null or 0)
            .Select(d =>
                d with
                {
                    Variantes = byParent.TryGetValue(d.OficinaMedioPagoId, out var kids)
                        ? kids
                        : Array.Empty<OficinaMedioPagoDto>(),
                })
            .ToList();

        if (soloRaices)
        {
            return roots;
        }

        return dtos
            .Select(d =>
                d with
                {
                    Variantes = byParent.TryGetValue(d.OficinaMedioPagoId, out var kids)
                        ? kids
                        : Array.Empty<OficinaMedioPagoDto>(),
                })
            .ToList();
    }

    public async Task<OficinaMedioPagoOperacionResponse> GuardarAsync(
        GuardarOficinaMedioPagoRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        await using var connection = new SqlConnection(_cs);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await OficinaMedioPagoSeedSql
                .EnsureSemillaOficinaAsync(
                    connection,
                    tx,
                    request.OficinaId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (request.ParentOficinaMedioPagoId is > 0)
            {
                var parentOficina = await connection
                    .ExecuteScalarAsync<int?>(
                        new CommandDefinition(
                            """
                            SELECT OficinaId
                            FROM CREDITO.OficinaMedioPago
                            WHERE OficinaMedioPagoId = @ParentId;
                            """,
                            new { ParentId = request.ParentOficinaMedioPagoId.Value },
                            transaction: tx,
                            cancellationToken: cancellationToken))
                    .ConfigureAwait(false);

                if (parentOficina is null)
                {
                    throw new InvalidOperationException("La variante padre no existe.");
                }

                if (parentOficina.Value != request.OficinaId)
                {
                    throw new InvalidOperationException(
                        "La variante debe pertenecer a la misma sucursal que el medio padre.");
                }
            }

            short tipoPagoId = request.TipoPagoId ?? 0;
            if (request.OficinaMedioPagoId is null or < 1)
            {
                if (tipoPagoId < 1)
                {
                    if (!request.CrearTipoPagoCatalogo)
                    {
                        throw new InvalidOperationException(
                            "Indique TipoPagoId o active CrearTipoPagoCatalogo para medios nuevos.");
                    }

                    tipoPagoId = await AllocTipoPagoIdAsync(
                            connection,
                            (SqlTransaction)tx,
                            request.Nombre,
                            request.OficinaId,
                            request.SoloCentral,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    var occupied = await connection
                        .ExecuteScalarAsync<int>(
                            new CommandDefinition(
                                """
                                SELECT COUNT(1)
                                FROM CREDITO.OficinaMedioPago
                                WHERE OficinaId = @OficinaId AND TipoPagoId = @TipoPagoId;
                                """,
                                new { request.OficinaId, TipoPagoId = tipoPagoId },
                                transaction: tx,
                                cancellationToken: cancellationToken))
                        .ConfigureAwait(false);

                    if (occupied > 0)
                    {
                        throw new InvalidOperationException(
                            $"La sucursal ya tiene un medio con TipoPagoId {tipoPagoId}.");
                    }
                }

                var newId = await connection
                    .ExecuteScalarAsync<int>(
                        new CommandDefinition(
                            """
                            INSERT INTO CREDITO.OficinaMedioPago (
                                OficinaId, ParentOficinaMedioPagoId, TipoPagoId, Nombre, MarcaCodigo, Orden, Activo,
                                EsDigital, UsaCelular, Telefono, UsaCuenta, NumeroCuenta, Cci,
                                UsaQr, QrEmvPayload, QrNota, FamiliaCodigo, SoloCentral
                            )
                            OUTPUT INSERTED.OficinaMedioPagoId
                            VALUES (
                                @OficinaId, @ParentOficinaMedioPagoId, @TipoPagoId, @Nombre, @MarcaCodigo, @Orden, @Activo,
                                @EsDigital, @UsaCelular, @Telefono, @UsaCuenta, @NumeroCuenta, @Cci,
                                @UsaQr, @QrEmvPayload, @QrNota, @FamiliaCodigo, @SoloCentral
                            );
                            """,
                            Bind(request, tipoPagoId),
                            transaction: tx,
                            cancellationToken: cancellationToken))
                    .ConfigureAwait(false);

                if (request.Activo)
                {
                    await EnsureBovedaCuentasAbiertasAsync(
                            connection,
                            (SqlTransaction)tx,
                            request.OficinaId,
                            tipoPagoId,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new OficinaMedioPagoOperacionResponse(newId, tipoPagoId);
            }

            var existingTipo = await connection
                .ExecuteScalarAsync<short?>(
                    new CommandDefinition(
                        """
                        SELECT TipoPagoId
                        FROM CREDITO.OficinaMedioPago
                        WHERE OficinaMedioPagoId = @Id AND OficinaId = @OficinaId;
                        """,
                        new { Id = request.OficinaMedioPagoId.Value, request.OficinaId },
                        transaction: tx,
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (existingTipo is null)
            {
                throw new InvalidOperationException("Medio de pago no encontrado en la oficina.");
            }

            // TipoPagoId del ledger no se mueve entre oficinas ni se renombra el catálogo global.
            tipoPagoId = existingTipo.Value;

            var affected = await connection
                .ExecuteAsync(
                    new CommandDefinition(
                        """
                        UPDATE CREDITO.OficinaMedioPago
                        SET
                            ParentOficinaMedioPagoId = @ParentOficinaMedioPagoId,
                            Nombre = @Nombre,
                            MarcaCodigo = @MarcaCodigo,
                            Orden = @Orden,
                            Activo = @Activo,
                            EsDigital = @EsDigital,
                            UsaCelular = @UsaCelular,
                            Telefono = @Telefono,
                            UsaCuenta = @UsaCuenta,
                            NumeroCuenta = @NumeroCuenta,
                            Cci = @Cci,
                            UsaQr = @UsaQr,
                            QrEmvPayload = @QrEmvPayload,
                            QrNota = @QrNota,
                            FamiliaCodigo = @FamiliaCodigo,
                            SoloCentral = @SoloCentral,
                            UpdatedAtUtc = SYSUTCDATETIME()
                        WHERE OficinaMedioPagoId = @OficinaMedioPagoId
                          AND OficinaId = @OficinaId;
                        """,
                        Bind(request, tipoPagoId, request.OficinaMedioPagoId),
                        transaction: tx,
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (affected != 1)
            {
                throw new InvalidOperationException("No se pudo actualizar el medio de pago.");
            }

            if (request.Activo)
            {
                await EnsureBovedaCuentasAbiertasAsync(
                        connection,
                        (SqlTransaction)tx,
                        request.OficinaId,
                        tipoPagoId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new OficinaMedioPagoOperacionResponse(request.OficinaMedioPagoId.Value, tipoPagoId);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<OficinaMedioPagoOperacionResponse> SetActivoAsync(
        int oficinaMedioPagoId,
        int oficinaId,
        bool activo,
        CancellationToken cancellationToken)
    {
        if (oficinaMedioPagoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaMedioPagoId));
        }

        if (oficinaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(oficinaId));
        }

        await using var connection = new SqlConnection(_cs);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var tipoPagoId = await connection
                .ExecuteScalarAsync<short?>(
                    new CommandDefinition(
                        """
                        UPDATE CREDITO.OficinaMedioPago
                        SET Activo = @Activo, UpdatedAtUtc = SYSUTCDATETIME()
                        OUTPUT INSERTED.TipoPagoId
                        WHERE OficinaMedioPagoId = @Id
                          AND OficinaId = @OficinaId;
                        """,
                        new { Id = oficinaMedioPagoId, OficinaId = oficinaId, Activo = activo },
                        transaction: tx,
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (tipoPagoId is null)
            {
                throw new InvalidOperationException("Medio de pago no encontrado en la oficina.");
            }

            if (activo)
            {
                await EnsureBovedaCuentasAbiertasAsync(
                        connection,
                        (SqlTransaction)tx,
                        oficinaId,
                        tipoPagoId.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new OficinaMedioPagoOperacionResponse(oficinaMedioPagoId, tipoPagoId.Value);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Abre BovedaCuenta en bóvedas abiertas de la oficina al activar/crear un medio.
    /// Paridad con EnsureBovedaCuenta de movimientos: SI cabecera → efectivo si no había cuentas.
    /// No borra al desactivar (historial / saldos previos).
    /// </summary>
    private static async Task EnsureBovedaCuentasAbiertasAsync(
        SqlConnection connection,
        SqlTransaction tx,
        int oficinaId,
        short tipoPagoId,
        CancellationToken cancellationToken)
    {
        await connection
            .ExecuteAsync(
                new CommandDefinition(
                    """
                    DECLARE @BovedaId int, @SiCab decimal(16, 2);

                    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
                    SELECT b.BovedaId, b.SaldoInicial
                    FROM CREDITO.Boveda AS b
                    WHERE b.OficinaId = @OficinaId
                      AND b.IndCierre = CAST(0 AS bit);

                    OPEN cur;
                    FETCH NEXT FROM cur INTO @BovedaId, @SiCab;
                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 FROM CREDITO.BovedaCuenta
                            WHERE BovedaId = @BovedaId AND TipoPagoId = @TipoPagoId)
                        BEGIN
                            IF NOT EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaId)
                            BEGIN
                                IF @TipoPagoId = 1
                                    INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
                                    VALUES (@BovedaId, 1, @SiCab);
                                ELSE
                                BEGIN
                                    INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
                                    VALUES (@BovedaId, 1, @SiCab);
                                    INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
                                    VALUES (@BovedaId, @TipoPagoId, CAST(0 AS decimal(16, 2)));
                                END
                            END
                            ELSE
                                INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
                                VALUES (@BovedaId, @TipoPagoId, CAST(0 AS decimal(16, 2)));
                        END

                        FETCH NEXT FROM cur INTO @BovedaId, @SiCab;
                    END
                    CLOSE cur;
                    DEALLOCATE cur;
                    """,
                    new { OficinaId = oficinaId, TipoPagoId = tipoPagoId },
                    transaction: tx,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<string?> ValidateMedioActivoAsync(
        int oficinaId,
        int tipoPagoId,
        CancellationToken cancellationToken,
        string fieldName = "tipoPagoId")
    {
        if (oficinaId < 1 || tipoPagoId < 1)
        {
            return $"{fieldName}: oficina/medio inválido.";
        }

        await using var connection = new SqlConnection(_cs);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await TipoPagoOficinaSql
            .RequireMedioActivoEnOficinaAsync(
                connection,
                oficinaId,
                tipoPagoId,
                cancellationToken,
                fieldName: fieldName)
            .ConfigureAwait(false);
    }

    private static async Task<short> AllocTipoPagoIdAsync(
        SqlConnection connection,
        SqlTransaction tx,
        string nombre,
        int oficinaId,
        bool soloCentral,
        CancellationToken cancellationToken)
    {
        var next = await connection
            .ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT ISNULL(MAX(ItemId), 13) + 1
                    FROM MAESTRO.ValorTabla
                    WHERE TablaId = 13;
                    """,
                    transaction: tx,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        if (next > short.MaxValue)
        {
            throw new InvalidOperationException("No hay ItemId disponible en ValorTabla (13).");
        }

        // DesCorta marca el Item como propio de una oficina: no contamina chips de otras sedes.
        var desCorta = soloCentral ? "CENTRAL" : $"OFI:{oficinaId}";

        await connection
            .ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.ValorTabla (TablaId, ItemId, Denominacion, DesCorta, Valor)
                    VALUES (13, @ItemId, @Nombre, @DesCorta, NULL);
                    """,
                    new
                    {
                        ItemId = (short)next,
                        Nombre = nombre.Trim(),
                        DesCorta = desCorta,
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return (short)next;
    }

    private static object Bind(
        GuardarOficinaMedioPagoRequest request,
        short tipoPagoId,
        int? oficinaMedioPagoId = null) =>
        new
        {
            OficinaMedioPagoId = oficinaMedioPagoId,
            request.OficinaId,
            request.ParentOficinaMedioPagoId,
            TipoPagoId = tipoPagoId,
            Nombre = request.Nombre.Trim(),
            MarcaCodigo = string.IsNullOrWhiteSpace(request.MarcaCodigo)
                ? "otro"
                : request.MarcaCodigo.Trim().ToLowerInvariant(),
            request.Orden,
            request.Activo,
            request.EsDigital,
            request.UsaCelular,
            Telefono = NormalizePhone(request.Telefono),
            request.UsaCuenta,
            NumeroCuenta = NullIfWhite(request.NumeroCuenta),
            Cci = NullIfWhite(request.Cci),
            request.UsaQr,
            QrEmvPayload = NullIfWhite(request.QrEmvPayload),
            QrNota = NullIfWhite(request.QrNota),
            FamiliaCodigo = NullIfWhite(request.FamiliaCodigo)?.ToUpperInvariant(),
            request.SoloCentral,
        };

    private static void Validate(GuardarOficinaMedioPagoRequest request)
    {
        if (request.OficinaId < 1)
        {
            throw new ArgumentException("OficinaId inválida.");
        }

        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 80)
        {
            throw new ArgumentException("Nombre obligatorio (máx. 80).");
        }

        // Configuración progresiva: flags sin dato aún están permitidos (cobro avisa si falta).
        if (!string.IsNullOrWhiteSpace(request.Telefono) && NormalizePhone(request.Telefono) is null)
        {
            throw new ArgumentException("Teléfono inválido: use 9 dígitos (ej. 9XXXXXXXX).");
        }

        if (!string.IsNullOrWhiteSpace(request.Cci))
        {
            var cciDigits = new string(request.Cci.Where(char.IsDigit).ToArray());
            if (cciDigits.Length is not (0 or 20))
            {
                throw new ArgumentException("CCI debe tener 20 dígitos.");
            }
        }
    }

    private static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("51", StringComparison.Ordinal) && digits.Length == 11)
        {
            digits = digits[2..];
        }

        return digits.Length == 9 ? digits : null;
    }

    private static string? NullIfWhite(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static OficinaMedioPagoDto ToDto(Row r) =>
        new(
            r.OficinaMedioPagoId,
            r.OficinaId,
            r.ParentOficinaMedioPagoId,
            r.TipoPagoId,
            r.Nombre,
            r.MarcaCodigo,
            r.Orden,
            r.Activo,
            r.EsDigital,
            r.UsaCelular,
            r.Telefono,
            r.UsaCuenta,
            r.NumeroCuenta,
            r.Cci,
            r.UsaQr,
            r.QrEmvPayload,
            r.QrNota,
            r.FamiliaCodigo,
            r.SoloCentral,
            Array.Empty<OficinaMedioPagoDto>());
}
