namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resuelve la oficina de una bóveda para autorizar lecturas por <c>BovedaId</c>.</summary>
public interface IBovedaOficinaReadService
{
    /// <summary><c>OficinaId</c> de <c>CREDITO.Boveda</c>, o <c>null</c> si no existe la fila.</summary>
    Task<int?> GetOficinaIdByBovedaIdAsync(int bovedaId, CancellationToken cancellationToken = default);

    /// <summary>Cabecera del RDLC <c>rptMovimientoBoveda</c> (saldos y estado).</summary>
    Task<BovedaAbiertaDto?> GetCabeceraReporteAsync(int bovedaId, CancellationToken cancellationToken = default);

    /// <summary>Paridad <c>BovedaController.ExisteBovedaTemporal</c>.</summary>
    Task<bool> ExisteBovedaTemporalAbiertaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);

    /// <summary>Bóvedas principales abiertas de otras oficinas, para combo de transferencia.</summary>
    Task<IReadOnlyList<BovedaDestinoTransferenciaDto>> ListarDestinosTransferenciaAsync(
        int oficinaOrigenId,
        CancellationToken cancellationToken = default);
}
