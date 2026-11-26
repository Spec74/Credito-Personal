namespace Credito.Modern.Api.Auth;

/// <summary>
/// Políticas de autorización. Los roles coinciden con <c>MAESTRO.Rol.Denominacion</c>
/// (claims <c>http://schemas.microsoft.com/ws/2008/06/identity/claims/role</c>).
/// <para>
/// Modelo multi-sucursal (rol × alcance):
/// <list type="bullet">
/// <item><b>ADMINISTRADOR</b> en Central (<c>IndPrincipal</c>): admin de red (lectura/escritura en toda la red).</item>
/// <item><b>ENCARGADO</b> (gerente de sede): gestión operativa e independencia de su oficina
/// (usuarios/cajas/medios/bóveda de su sede). Incluso en Central solo opera esa oficina.</item>
/// <item><b>APROBADOR</b>: bandeja y ciclo de aprobación (alias legacy APROBADOR 1 / 2).</item>
/// </list>
/// El alcance sede vs red lo da la oficina de sesión + si el actor es administrador;
/// no hace falta un segundo código ADMIN_SEDE.
/// </para>
/// </summary>
public static class CreditoAuthorizationPolicies
{
    public const string CreditoUser = "CreditoUser";
    public const string CreditoRolAdministrador = "CreditoRolAdministrador";
    public const string CreditoRolEncargado = "CreditoRolEncargado";

    /// <summary>Legacy name; usa <see cref="HasAprobador"/> (APROBADOR unificado).</summary>
    public const string CreditoRolAprobador1 = "CreditoRolAprobador1";

    /// <summary>APROBADOR (o legacy 1/2) u ADMINISTRADOR.</summary>
    public const string CreditoRolAprobador1OAdministrador = "CreditoRolAprobador1OAdministrador";

    /// <summary>Alias semántico de <see cref="CreditoRolAprobador1OAdministrador"/>.</summary>
    public const string CreditoRolAprobadorOAdministrador = "CreditoRolAprobadorOAdministrador";

    /// <summary>Solo ADMINISTRADOR o ADMIN (reprogramar, condonar, catálogo de roles).</summary>
    public const string CreditoRolSoloAdministrador = "CreditoRolSoloAdministrador";

    /// <summary>ENCARGADO u ADMINISTRADOR (gestión de sede / ops + admin).</summary>
    public const string CreditoRolEncargadoOAdministrador = "CreditoRolEncargadoOAdministrador";

    /// <summary>ADMINISTRADOR, ADMIN o ANULACION_MOV.</summary>
    public const string CreditoRolAnularMovimientoCaja = "CreditoRolAnularMovimientoCaja";

    /// <summary>Escritura operativa de crédito/caja.</summary>
    public const string CreditoRolOperador = "CreditoRolOperador";

    /// <summary>Autenticado sin rol LECTURA_SALDO.</summary>
    public const string CreditoNoLecturaSaldo = "CreditoNoLecturaSaldo";

    /// <summary>ANALISTA o ADMINISTRADOR (prendario).</summary>
    public const string CreditoRolPrendario = "CreditoRolPrendario";

    public const string RolDenominacionAdministrador = "ADMINISTRADOR";
    public const string RolDenominacionAdminLegacy = "ADMIN";
    public const string RolDenominacionEncargado = "ENCARGADO";
    public const string RolDenominacionAprobador = "APROBADOR";
    public const string RolDenominacionAprobador1 = "APROBADOR 1";
    public const string RolDenominacionAprobador2 = "APROBADOR 2";
    public const string RolDenominacionLectura = "LECTURA";
    public const string RolDenominacionAnalista = "ANALISTA";
    public const string RolDenominacionGestor = "GESTOR";
    public const string RolDenominacionCaja = "CAJA";
    public const string RolDenominacionCajero = "CAJERO";
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

    public static readonly string[] RolesAprobadorOAdministrador =
    [
        RolDenominacionAprobador,
        RolDenominacionAprobador1,
        RolDenominacionAprobador2,
        RolDenominacionAdministrador,
        RolDenominacionAdminLegacy,
    ];

    /// <summary>Compat: mismo conjunto que <see cref="RolesAprobadorOAdministrador"/>.</summary>
    public static readonly string[] RolesAprobador1OAdministrador = RolesAprobadorOAdministrador;

    public static readonly string[] RolesEncargadoOAdministrador =
    [
        RolDenominacionEncargado,
        RolDenominacionAdministrador,
        RolDenominacionAdminLegacy,
    ];

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

    /// <summary>Roles que solo un ADMINISTRADOR puede asignar (no el gerente de sede).</summary>
    public static readonly string[] RolesReservadosAdminRed =
    [
        RolDenominacionAdministrador,
        RolDenominacionAdminLegacy,
    ];

    public static bool HasAdministrador(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionAdministrador)
        || HasExact(roles, RolDenominacionAdminLegacy);

    /// <summary>APROBADOR unificado (incluye legacy 1/2 y alias cortos).</summary>
    public static bool HasAprobador(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            var n = NormalizeRole(role);
            if (n is "APROBADOR"
                or "APROBADOR1" or "APROBADOR01" or "APRO1" or "APRO01"
                or "APROBADOR2" or "APROBADOR02" or "APRO2" or "APRO02")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Compat: mismo que <see cref="HasAprobador"/>.</summary>
    public static bool HasAprobador1(IEnumerable<string> roles) => HasAprobador(roles);

    /// <summary>Compat legacy: APROBADOR 2 ya no es un nivel distinto.</summary>
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

    public static bool HasAprobador1OAdministrador(IEnumerable<string> roles) =>
        HasAprobador(roles) || HasAdministrador(roles);

    public static bool HasAprobadorOAdministrador(IEnumerable<string> roles) =>
        HasAprobador1OAdministrador(roles);

    public static bool HasEncargado(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionEncargado)
        || HasTokenPrefix(roles, RolDenominacionEncargado);

    public static bool HasAnalista(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionAnalista)
        || HasTokenPrefix(roles, RolDenominacionAnalista);

    public static bool HasGestor(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionGestor)
        || HasTokenPrefix(roles, RolDenominacionGestor);

    public static bool HasCajaOperativa(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            var normalized = NormalizeRole(role);
            if (normalized is "CAJA" or "CAJERO" or "CAJACENTRAL")
            {
                return true;
            }

            if (normalized.StartsWith("CAJA", StringComparison.Ordinal)
                || normalized.StartsWith("CAJERO", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasEncargadoOAdministrador(IEnumerable<string> roles) =>
        HasEncargado(roles) || HasAdministrador(roles);

    public static bool HasAnulacionMovimientoCaja(IEnumerable<string> roles) =>
        HasAdministrador(roles) || HasExact(roles, RolDenominacionAnulacionMov);

    public static bool HasRolOperador(IEnumerable<string> roles) =>
        HasAdministrador(roles)
        || HasEncargado(roles)
        || HasAnalista(roles)
        || HasGestor(roles)
        || HasCajaOperativa(roles);

    public static bool HasPrendario(IEnumerable<string> roles) =>
        HasAnalista(roles) || HasAdministrador(roles);

    public static bool HasSoloLectura(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionLectura);

    public static bool HasLecturaSaldo(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionLecturaSaldo);

    public static bool HasSaldosCaja(IEnumerable<string> roles) =>
        HasExact(roles, RolDenominacionSaldosCaja);

    public static bool EsRolReservadoAdminRed(string? denominacion)
    {
        var n = NormalizeRole(denominacion);
        return n is "ADMINISTRADOR" or "ADMIN";
    }

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
