namespace Credito.Modern.Application.CreditoPlanes;

public interface IMovimientoCajaDetalleOvReadService
{
    Task<MovimientoCajaDetalleOvDto?> GetAsync(
        int movimientoCajaId,
        CancellationToken cancellationToken = default);
}

/// <summary>Paridad <c>CajaDiarioBL.MostrarDetalleOvMovCaja</c>.</summary>
public sealed record MovimientoCajaDetalleOvDto(
    int MovimientoCajaId,
    int OficinaId,
    string Operacion,
    IReadOnlyList<string> Lineas);
