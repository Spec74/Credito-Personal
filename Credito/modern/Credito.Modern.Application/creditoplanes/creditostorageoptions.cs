namespace Credito.Modern.Application.CreditoPlanes;

public sealed class CreditoStorageOptions
{
    public const string SectionName = "CreditoStorage";

    /// <summary>Ruta raíz de evidencias (paridad ~/storage/ del MVC). Vacío = {ContentRoot}/storage.</summary>
    public string? RootPath { get; init; }
}
