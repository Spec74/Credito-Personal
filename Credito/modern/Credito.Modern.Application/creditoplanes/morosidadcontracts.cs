namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>ACL del módulo Morosos empresarial (paridad AppSettings Morosidad.UsuarioConsultaIds).</summary>
public sealed class MorosidadOptions
{
    public const string SectionName = "Morosidad";

    /// <summary>Usuarios autorizados a consultar / exportar (paridad legado).</summary>
    public int[] UsuarioConsultaIds { get; set; } = [3, 10];

    /// <summary>
    /// Si true, ADMIN/ADMINISTRADOR entran aunque no estén en la lista.
    /// Por defecto false: paridad MVC (_Layout solo mira UsuarioConsultaIds).
    /// </summary>
    public bool PermitirAdministradores { get; set; }
}

public sealed class MorosidadEmpresaItemDto
{
    public int PersonaId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Celular { get; set; } = string.Empty;
    public int? OficinaId { get; set; }
    public string Oficina { get; set; } = string.Empty;
    public int? GestorId { get; set; }
    public string GestorUsuario { get; set; } = string.Empty;
    public string GestorNombre { get; set; } = string.Empty;
    public int CreditosMora { get; set; }
    public decimal SaldoMora { get; set; }
    public DateTime? PrimeraCuotaVencida { get; set; }
    public DateTime? FechaUltimoPago { get; set; }
    public int DiasAtraso { get; set; }
    public string CodigoClasificacion { get; set; } = string.Empty;
    public string Clasificacion { get; set; } = string.Empty;
    public DateTime FechaCorte { get; set; }
}

public sealed record MorosidadResumenDto(
    int Clientes,
    int Creditos,
    decimal Saldo,
    int NuncaPagaron,
    int DejaronPagar,
    int PaganConAtraso);

public sealed record MorosidadEmpresaResponse(
    bool Success,
    string FechaCorte,
    MorosidadResumenDto Resumen,
    IReadOnlyList<MorosidadEmpresaItemDto> Filas);

public sealed record MorosidadPermisosResponse(bool PuedeConsultar);

public interface IMorosidadAccessService
{
    bool PuedeConsultar(int usuarioId, IEnumerable<string> roles);
}

public interface IMorosidadReadService
{
    Task<IReadOnlyList<MorosidadEmpresaItemDto>> ObtenerMorososEmpresaAsync(
        string tipo = "TODOS",
        int? oficinaId = null,
        int? gestorId = null,
        DateTime? fechaCorte = null,
        CancellationToken cancellationToken = default);
}
