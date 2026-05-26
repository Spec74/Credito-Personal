namespace Credito.Modern.Api.Auth;



/// <summary>Nombres de políticas de autorización. Los roles coinciden con <c>MAESTRO.Rol.Denominacion</c> del legado (claims <c>http://schemas.microsoft.com/ws/2008/06/identity/claims/role</c>).</summary>

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



    /// <summary>Escritura de crédito: usuario autenticado con al menos un rol distinto de LECTURA.</summary>

    public const string CreditoRolOperador = "CreditoRolOperador";



    /// <summary>Denominación en BD / claim <see cref="System.Security.Claims.ClaimTypes.Role"/>.</summary>

    public const string RolDenominacionAdministrador = "ADMINISTRADOR";



    public const string RolDenominacionAdminLegacy = "ADMIN";



    public const string RolDenominacionEncargado = "ENCARGADO";



    public const string RolDenominacionAprobador1 = "APROBADOR 1";



    public const string RolDenominacionLectura = "LECTURA";



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

}


