namespace Credito.Modern.Application.CreditoPlanes;

public interface IVoucherValidacionService
{
    /// <summary>
    /// OCR de una constancia (foto al celular) → monto, fecha/hora, código y canal.
    /// </summary>
    Task<VoucherValidacionResponse> ValidarCapturaAsync(
        Stream image,
        string fileName,
        decimal? montoEsperado,
        string? canalEsperado = null,
        CancellationToken cancellationToken = default);
}

public sealed record VoucherValidacionResponse(
    bool Ok,
    string? CanalDetectado,
    decimal? Monto,
    DateTime? FechaOperacion,
    string? FechaOperacionIso,
    string? FechaOperacionLocal,
    string? CodigoOperacion,
    bool CoincideMonto,
    bool OcrDisponible,
    IReadOnlyList<string> Advertencias,
    string? TextoOcrPreview);
