namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>CajaDiarioBL.LstCreditoPendienteJGrid</c> + campos de ruta cobro diario.</summary>
public sealed class CreditoGestorPendienteRowDto
{
    public int CreditoId { get; set; }
    public string PersonaCodigo { get; set; } = string.Empty;
    public string PersonaNombre { get; set; } = string.Empty;
    public decimal MontoCredito { get; set; }
    public int PersonaId { get; set; }
    /// <summary>Paridad <c>CreditoPendienteJGrid.FechaVencimiento</c> / orden cobro diario.</summary>
    public DateTime FechaVencimiento { get; set; }
    /// <summary>Paridad <c>CreditoPendienteJGrid.ImporteMora</c> (mora total no CAN).</summary>
    public decimal ImporteMora { get; set; }
    /// <summary>
    /// Paridad <c>CreditoPendienteJGrid.DeudaPendiente</c>: capital pendiente + mora total (no CAN).
    /// </summary>
    public decimal DeudaPendiente { get; set; }

    /// <summary>Paridad <c>usp_RptCobroDiario.Orden</c> (número derivado del código persona).</summary>
    public int? Orden { get; set; }

    public string? Celular { get; set; }

    public string? Direccion { get; set; }

    /// <summary>GPS del cliente (<c>MAESTRO.Cliente.Latitud</c>), si está registrado.</summary>
    public decimal? Latitud { get; set; }

    /// <summary>GPS del cliente (<c>MAESTRO.Cliente.Longitud</c>), si está registrado.</summary>
    public decimal? Longitud { get; set; }

    /// <summary>
    /// Atajo cobro en campo: cuotas vencidas acumuladas, o la próxima pendiente
    /// (estado distinto de PAG/CAN; incluye cargo/mora de cuota).
    /// </summary>
    public decimal CuotaSugerida { get; set; }

    /// <summary>Días de atraso de la cuota PEN más antigua (0 si al día).</summary>
    public int DiasAtrazo { get; set; }
}
