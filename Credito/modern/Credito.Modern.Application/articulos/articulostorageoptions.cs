namespace Credito.Modern.Application.Articulos;

public sealed class ArticuloStorageOptions
{
    public const string SectionName = "ArticuloStorage";

    /// <summary>Ruta raíz de imágenes (paridad ~/imgArticulos del MVC). Vacío = {BaseDirectory}/imgArticulos.</summary>
    public string? RootPath { get; init; }
}
