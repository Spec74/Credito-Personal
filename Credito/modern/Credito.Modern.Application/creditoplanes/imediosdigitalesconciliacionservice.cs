namespace Credito.Modern.Application.CreditoPlanes;

public interface IMediosDigitalesConciliacionService
{
    Task<MedioDigitalConciliacionResultadoDto> ConciliarYapeAsync(
        int oficinaId,
        Stream archivo,
        string fileName,
        bool aplicarAutos,
        CancellationToken cancellationToken = default);

    Task<MedioDigitalConciliacionResultadoDto> ConciliarPlinCapturasAsync(
        int oficinaId,
        IReadOnlyList<(Stream Stream, string FileName)> capturas,
        bool aplicarAutos,
        CancellationToken cancellationToken = default);
}

public interface IOcrEngine
{
    /// <summary>Extrae texto de una imagen (jpg/png/webp). Vacío si no hay motor OCR.</summary>
    Task<string> ExtractTextAsync(Stream image, string fileName, CancellationToken cancellationToken = default);

    bool IsAvailable { get; }
}
