namespace Credito.Modern.Application.Ventas;

/// <summary>
/// Caja diario abierta del usuario para pantalla venta rápida (<c>VentaRapidaController.Index</c>).
/// </summary>
public sealed record CajaDiarioVentaRapidaDto(
    int CajaDiarioId,
    int CajaId,
    string CajaDenominacion,
    DateTime FechaIniOperacion);
