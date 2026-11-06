namespace Credito.Modern.Api.Auth;

/// <summary>
/// Nombres de políticas de autorización. Los roles coinciden con <c>MAESTRO.Rol.Denominacion</c>
/// del legado (claims <c>http://schemas.microsoft.com/ws/2008/06/identity/claims/role</c>).
/// <para>
/// Multisucursal: el alcance sede vs red lo da la oficina de sesión (<c>IndPrincipal</c> → Central),
/// no un segundo código de rol. Las denominaciones se mantienen estables; la UI muestra etiquetas
/// amigables (Administrador de sede / de red).
/// </para>
/// </summary>
public static class CreditoAuthorizationPolicies
{
    public const string CreditoUser = "CreditoUser";
    public const string CreditoRolAdministrador = "CreditoRolAdministrador";
    public const string CreditoRolEncargado = "CreditoRolEncargado";
    public const string CreditoRolAprobador1 = "CreditoRolAprobador1";

    /// <summary>APROBADOR 1, ADMINISTRADOR o ADMIN (informes / tope / ciclo aprobación).</summary>
    public const string CreditoRolAprobador1OAdministrador = "CreditoRolAprobador1OAdministrador";

    /// <summary>Solo ADMINISTRADOR o ADMIN (reprogramar, condonar — paridad Creditos.cshtml).</summary>
    public const string CreditoRolSoloAdministrador = "CreditoRolSoloAdministrador";

    /// <summary>ENCARGADO, ADMINISTRADOR o ADMIN (bóveda temporal, asignaciones).</summary>
    public const string CreditoRolEncargadoOAdministrador = "CreditoRolEncargadoOAdministrador";

    /// <summary>ADMINISTRADOR, ADMIN o ANULACION_MOV (paridad SaldosController).</summary>
    public const string CreditoRolAnularMovimientoCaja = "CreditoRolAnularMovimientoCaja";

    /// <summary>Escritura de crédito: roles operativos reales, no permisos solo lectura/reporte.</summary>
    public const string CreditoRolOperador = "CreditoRolOperador";

    /// <summary>Autenticado sin rol LECTURA_SALDO (asignar/cerrar en Saldos, paridad ViewBag.EsLectura).</summary>
    public const string CreditoNoLecturaSaldo = "CreditoNoLecturaSaldo";

    /// <summary>
    /// ANALISTA o ADMINISTRADOR. El módulo prendario es del analista por pedido de gerencia; el
    /// administrador entra porque de todos modos puede concederse el menú desde Seguridad.
    /// En el legacy el controlador solo exigía estar autenticado y el control quedaba en el menú.
    /// </summary>
    public const string CreditoRolPrendario = "CreditoRolPrendario";

    public const string RolDenominacionAdministrador = "ADMINISTRADOR";
    public const string RolDenominacionAdminLegacy = "ADMIN";
    public const string RolDenominacionEncargado = "ENCARGADO";
    public const string RolDenominacionAprobador1 = "APROBADOR 1";
    public const string RolDenominacionAprobador2 = "APROBADOR 2";
    public const string RolDenominacionLectura = "LECTURA";
    public const string RolDenominacionAnalista = "ANALISTA";
    public const string RolDenominacionGestor = "GESTOR";
    /// <summary>Denominación viva en <c>MAESTRO.Rol</c> (cajero de sede).</summary>
    public const string RolDenominacionCaja = "CAJA";
    /// <summary>Alias histórico / go-live; no siempre existe como fila en Rol.</summary>
    public const string RolDenominacionCajero = "CAJERO";
    /// <summary>
    /// Rol de menú adicional para operadores de caja central. La identidad «caja central»
    /// operativa también vive en <c>MAESTRO.Caja.Denominacion</c> (substring CAJA CENTRAL);
    /// no confundir rol con maestro de caja.
    /// </summary>
    public const string RolDenominacionCajaCentral = "CAJA CENTRAL";
    public const string RolDenominacionReporteParcial = "REPORTEPARCIAL";
    public const string RolDenominacionLecturaSaldo = "LECTURA_SALDO";
    public const string RolDenominacionSaldosCaja = "SALDOS CAJA";
    public const string RolDenominacionAnulacionMov = "ANULACION_MOV";

    public static readonly string[] RolesAdministrador =
    [
        RolDenominacionAdministrador,
        RolDenominacionAdminLegacy,
    ];

    public static readonly string[] RolesAprobador1OAdministrador =
    [
        RolDenominacionAprobador1,
        RolDenominacionAdministrador,
        RolDenominacionAdminLegacy,
    ];

    public static readonly string[] RolesEncargadoOAdministrador =
    [
        RolDenominacionEncargado,
        RolDenominacionAdministrador,
        RolDenominacionAdminLegacy,
    ];

    /// <summary>Roles con escritura operativa de crédito/caja (política CreditoRolOperador).</summary>
    public static readonly string[] RolesOperador =
    [
        RolDenominacionAdministrador,
        RolDenominacionAdminLegacy,
        RolDenominacionEncargado,
        RolDenominacionAnalista,
        RolDenominacionGestor,
        RolDenominacionCaja,
        RolDenominacionCajero,
        RolDenominacionCajaCentral,
    ];

    public static bool HasAdministrador(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionAdministrador)
        || HasExact(roles, RolDenominacionAdminLegacy);

    public static bool HasAprobador1(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            // Aliases normalizados (espacios ya removidos). No incluye APROBADOR 2.
            if (NormalizeRole(role) is "APROBADOR1" or "APROBADOR01" or "APRO1" or "APRO01")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// APROBADOR 2: segundo nivel / consulta de bandeja y reportes. No sustituye a APROBADOR 1
    /// en escrituras de aprobación (política CreditoRolAprobador1*).
    /// </summary>
    public static bool HasAprobador2(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            if (NormalizeRole(role) is "APROBADOR2" or "APROBADOR02" or "APRO2" or "APRO02")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Cualquier nivel de aprobador (1 o 2).</summary>
    public static bool HasAprobador(IEnumerable<string> roles) =>
        HasAprobador1(roles) || HasAprobador2(roles);

    public static bool HasEncargado(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionEncargado)
        || HasTokenPrefix(roles, RolDenominacionEncargado);

    public static bool HasAnalista(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionAnalista)
        || HasTokenPrefix(roles, RolDenominacionAnalista);

    public static bool HasGestor(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionGestor)
        || HasTokenPrefix(roles, RolDenominacionGestor);

    /// <summary>
    /// Operador de caja: denominación viva <c>CAJA</c>, alias <c>CAJERO</c>, y menú
    /// <c>CAJA CENTRAL</c>. La caja física «central» se identifica aparte por nombre en maestro.
    /// </summary>
    public static bool HasCajaOperativa(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            var normalized = NormalizeRole(role);
            if (normalized is "CAJA" or "CAJERO" or "CAJACENTRAL")
            {
                return true;
            }

            // Variantes: "CAJA CENTRAL 1" como rol (poco habitual) o "CAJERO …".
            if (normalized.StartsWith("CAJA", StringComparison.Ordinal)
                || normalized.StartsWith("CAJERO", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasAprobador1OAdministrador(IEnumerable<string> roles) =>
        HasAprobador1(roles) || HasAdministrador(roles);

    public static bool HasEncargadoOAdministrador(IEnumerable<string> roles) =>
        HasEncargado(roles) || HasAdministrador(roles);

    public static bool HasAnulacionMovimientoCaja(IEnumerable<string> roles) =>
        HasAdministrador(roles) || HasExact(roles, RolDenominacionAnulacionMov);

    /// <summary>
    /// Paridad operativa: roles de operación/caja/gestión. Excluye roles parciales usados solo para informes,
    /// lectura de saldos o anulación de movimientos.
    /// Incluye la denominación viva <c>CAJA</c> (antes solo se miraba el alias <c>CAJERO</c>).
    /// </summary>
    public static bool HasRolOperador(IEnumerable<string> roles) =>
        HasAdministrador(roles)
        || HasEncargado(roles)
        || HasAnalista(roles)
        || HasGestor(roles)
        || HasCajaOperativa(roles);

    public static bool HasPrendario(IEnumerable<string> roles) =>
        HasAnalista(roles) || HasAdministrador(roles);

    /// <summary>Solo lectura de crédito (exacto). No incluye LECTURA_SALDO.</summary>
    public static bool HasSoloLectura(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionLectura);

    public static bool HasLecturaSaldo(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionLecturaSaldo);

    public static bool HasSaldosCaja(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionSaldosCaja);

    /// <summary>Normaliza denominación de rol para comparación (sin espacios, mayúsculas).</summary>
    public static string NormalizeRole(string? role) =>
        string.IsNullOrWhiteSpace(role)
            ? string.Empty
            : role.Trim().ToUpperInvariant().Replace(" ", string.Empty);

    private static bool HasExact(IEnumerable<string> roles, string expected)
    {
        var expectedNormalized = NormalizeRole(expected);
        foreach (var role in roles)
        {
            if (NormalizeRole(role) == expectedNormalized)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Coincide si el claim es exacto o empieza por el token seguido de más texto ya sin espacios
    /// (p. ej. ENCARGADODESEDE). No usa prefijos cortos ambiguos (LECTURA ≠ LECTURA_SALDO).
    /// </summary>
    private static bool HasTokenPrefix(IEnumerable<string> roles, string expected)
    {
        var expectedNormalized = NormalizeRole(expected);
        if (expectedNormalized.Length < 4)
        {
            return false;
        }

        foreach (var role in roles)
        {
            var normalized = NormalizeRole(role);
            if (normalized.Length > expectedNormalized.Length
                && normalized.StartsWith(expectedNormalized, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
