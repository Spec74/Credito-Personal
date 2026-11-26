namespace Credito.Modern.Application.MediosPago;

public interface IOficinaMedioPagoService
{
    Task<IReadOnlyList<OficinaMedioPagoDto>> ListByOficinaAsync(
        int oficinaId,
        bool incluirInactivos,
        bool soloRaices,
        CancellationToken cancellationToken);

    Task<OficinaMedioPagoOperacionResponse> GuardarAsync(
        GuardarOficinaMedioPagoRequest request,
        CancellationToken cancellationToken);

    Task<OficinaMedioPagoOperacionResponse> SetActivoAsync(
        int oficinaMedioPagoId,
        int oficinaId,
        bool activo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Null si el TipoPagoId está activo en la oficina; mensaje de error si no.
    /// Asegura semilla base antes de validar.
    /// </summary>
    Task<string?> ValidateMedioActivoAsync(
        int oficinaId,
        int tipoPagoId,
        CancellationToken cancellationToken,
        string fieldName = "tipoPagoId");
}
