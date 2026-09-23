namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>ACL del módulo gerencial (paridad AppSettings legacy, sin hardcode en código).</summary>
public sealed class CierreGerencialOptions
{
    public const string SectionName = "CierreGerencial";

    /// <summary>Usuarios autorizados a consultar avance/excel (paridad AppSettings legado).</summary>
    public int[] UsuarioConsultaIds { get; set; } = [3, 10];

    /// <summary>Usuarios autorizados a editar metas (paridad AppSettings legado).</summary>
    public int[] UsuarioMetasIds { get; set; } = [10];

    /// <summary>
    /// Si true, ADMIN/ADMINISTRADOR entran aunque no estén en las listas.
    /// Por defecto false: paridad MVC (_Layout solo mira UsuarioConsultaIds).
    /// </summary>
    public bool PermitirAdministradores { get; set; }
}

public sealed class MetaGerencialDefinitivaDto
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public DateTime Periodo { get; set; }
    public decimal CapitalBase { get; set; }
    public int ClientesActivosBase { get; set; }
    public decimal? VencidosBaseComparable { get; set; }
    public decimal VencidosBaseLegacy { get; set; }
    public int? ClientesVencidosBase { get; set; }
    public decimal? MetaCapitalCierre { get; set; }
    public int? MetaClientesActivosCierre { get; set; }
    public decimal? MetaVencidosMaximoCierre { get; set; }
    public decimal? MetaRecuperacionVencidosMes { get; set; }
    public bool Configurada { get; set; }
    public bool PeriodoCerrado { get; set; }
    public bool PuedeEditar { get; set; }
    public DateTime FechaLimiteEdicion { get; set; }
    public string Asesor { get; set; } = string.Empty;
    public string Supervisor { get; set; } = string.Empty;
    public string Mercado { get; set; } = string.Empty;
    public short? Orden { get; set; }
    public string TipoCartera { get; set; } = string.Empty;
}

public sealed class AvanceMetaGerencialDto
{
    public short? Orden { get; set; }
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Asesor { get; set; } = string.Empty;
    public string Supervisor { get; set; } = string.Empty;
    public string Mercado { get; set; } = string.Empty;
    public string TipoCartera { get; set; } = string.Empty;
    public DateTime Periodo { get; set; }
    public decimal? CapitalBase { get; set; }
    public decimal? MetaCapitalCierre { get; set; }
    public decimal CapitalActual { get; set; }
    public decimal? DiferenciaCapital { get; set; }
    public decimal? CumplimientoCapitalPct { get; set; }
    public string EstadoCapital { get; set; } = string.Empty;
    public int? ClientesBase { get; set; }
    public int? MetaClientesActivosCierre { get; set; }
    public int ClientesActivosActual { get; set; }
    public int? DiferenciaClientes { get; set; }
    public decimal? CumplimientoClientesPct { get; set; }
    public string EstadoClientes { get; set; } = string.Empty;
    public decimal? VencidosBaseComparable { get; set; }
    public decimal? VencidosBaseLegacy { get; set; }
    public decimal? MetaVencidosMaximoCierre { get; set; }
    public decimal VencidosActual { get; set; }
    public decimal? MargenVencidos { get; set; }
    public string EstadoVencidos { get; set; } = string.Empty;
    public int? ClientesVencidosBase { get; set; }
    public int ClientesVencidosActual { get; set; }
    public decimal ClientesVencidosPct { get; set; }
    public decimal? MetaRecuperacionVencidosMes { get; set; }
    public decimal? RecuperacionVencidosActual { get; set; }
    public string EstadoRecuperacion { get; set; } = string.Empty;
    public bool MetaConfigurada { get; set; }
    public DateTime FechaCalculo { get; set; }
    public bool AvanceNoOficial { get; set; }
    public int ClientesNuevosMes { get; set; }
    public decimal MontoClientesNuevosMes { get; set; }
    public decimal MontoCobradoMes { get; set; }
    public decimal DesembolsosMes { get; set; }
    public int NroOperacionesMes { get; set; }
}

public sealed record MetaGerencialGuardarItemDto(
    int UsuarioId,
    string? TipoCartera,
    decimal? MetaCapitalCierre,
    int? MetaClientesActivosCierre,
    decimal? MetaVencidosMaximoCierre,
    decimal? MetaRecuperacionVencidosMes);

public sealed record GuardarMetasGerencialesRequest(
    DateTime Periodo,
    IReadOnlyList<MetaGerencialGuardarItemDto> Metas);

public sealed record CierreGerencialAvanceResponse(
    string Periodo,
    string? FechaCalculo,
    bool AvanceNoOficial,
    int Total,
    IReadOnlyList<AvanceMetaGerencialDto> Filas);

public sealed record CierreGerencialMetasResponse(
    string Periodo,
    bool PeriodoCerrado,
    bool PuedeEditar,
    string? FechaLimiteEdicion,
    IReadOnlyList<MetaGerencialDefinitivaDto> Metas);

public sealed record CierreGerencialPermisosResponse(
    bool PuedeConsultar,
    bool PuedeGestionarMetas);

public interface ICierreGerencialAccessService
{
    bool PuedeConsultar(int usuarioId, IEnumerable<string> roles);
    bool PuedeGestionarMetas(int usuarioId, IEnumerable<string> roles);
}

public interface ICierreGerencialReadService
{
    Task<IReadOnlyList<AvanceMetaGerencialDto>> ObtenerAvanceAsync(
        DateTime periodo,
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MetaGerencialDefinitivaDto>> ListarMetasAsync(
        DateTime periodo,
        CancellationToken cancellationToken = default);
}

public interface ICierreGerencialWriteService
{
    Task GuardarMetasAsync(
        DateTime periodo,
        IReadOnlyList<MetaGerencialGuardarItemDto> metas,
        int usuarioRegistroId,
        CancellationToken cancellationToken = default);
}
