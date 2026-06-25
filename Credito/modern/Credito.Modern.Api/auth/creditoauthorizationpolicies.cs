namespace Credito.Modern.Api.Auth;

/// <summary>
/// Nombres de políticas de autorización. Los roles coinciden con <c>MAESTRO.Rol.Denominacion</c>
/// del legado (claims <c>http://schemas.microsoft.com/ws/2008/06/identity/claims/role</c>).
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

    public const string RolDenominacionAdministrador = "ADMINISTRADOR";
    public const string RolDenominacionAdminLegacy = "ADMIN";
    public const string RolDenominacionEncargado = "ENCARGADO";
    public const string RolDenominacionAprobador1 = "APROBADOR 1";
    public const string RolDenominacionLectura = "LECTURA";
    public const string RolDenominacionAnalista = "ANALISTA";
    public const string RolDenominacionGestor = "GESTOR";
    public const string RolDenominacionCajero = "CAJERO";
    public const string RolDenominacionReporteParcial = "REPORTEPARCIAL";
    public const string RolDenominacionLecturaSaldo = "LECTURA_SALDO";
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

    public static bool HasAdministrador(IEnumerable<string> roles) =>
        HasRoleLike(roles, RolDenominacionAdministrador)
        || HasRoleLike(roles, RolDenominacionAdminLegacy);

    public static bool HasAprobador1(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            var normalized = NormalizeRole(role);
            if (normalized is "APROBADOR1" or "APROBADOR01" or "APRO1" or "APRO01")
            {
                return true;
            }

            if (normalized.StartsWith("APROBADOR", StringComparison.Ordinal)
                && (normalized.EndsWith("1", StringComparison.Ordinal)
                    || normalized.EndsWith("01", StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasEncargado(IEnumerable<string> roles) =>
        HasRoleLike(roles, RolDenominacionEncargado);

    public static bool HasAprobador1OAdministrador(IEnumerable<string> roles) =>
        HasAprobador1(roles) || HasAdministrador(roles);

    public static bool HasEncargadoOAdministrador(IEnumerable<string> roles) =>
        HasEncargado(roles) || HasAdministrador(roles);

    public static bool HasAnulacionMovimientoCaja(IEnumerable<string> roles) =>
        HasAdministrador(roles) || HasRoleLike(roles, RolDenominacionAnulacionMov);

    /// <summary>
    /// Paridad operativa: roles de operación/caja/gestión. Excluye roles parciales usados solo para informes,
    /// lectura de saldos o anulación de movimientos.
    /// </summary>
    public static bool HasRolOperador(IEnumerable<string> roles) =>
        HasAdministrador(roles)
        || HasEncargado(roles)
        || HasRoleLike(roles, RolDenominacionAnalista)
        || HasRoleLike(roles, RolDenominacionGestor)
        || HasRoleLike(roles, RolDenominacionCajero);

    public static bool HasSoloLectura(IEnumerable<string> roles) =>
        HasRoleLike(roles, RolDenominacionLectura);

    private static bool HasRoleLike(IEnumerable<string> roles, string expected)
    {
        var expectedNormalized = NormalizeRole(expected);
        foreach (var role in roles)
        {
            var normalized = NormalizeRole(role);
            if (normalized == expectedNormalized
                || normalized.StartsWith(expectedNormalized, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeRole(string? role) =>
        string.IsNullOrWhiteSpace(role)
            ? string.Empty
            : role.Trim().ToUpperInvariant().Replace(" ", string.Empty);
}
