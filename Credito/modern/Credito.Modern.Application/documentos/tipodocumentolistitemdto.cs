namespace Credito.Modern.Application.Documentos;

/// <summary>Fila de <c>MAESTRO.TipoDocumento</c> para listados.</summary>
public sealed class TipoDocumentoListItemDto
{
    public int TipoDocumentoId { get; set; }
    public string? Denominacion { get; set; }
    public bool IndVenta { get; set; }
    public bool IndAlmacen { get; set; }
    public bool IndAlmacenMov { get; set; }
    public bool IndCajaChica { get; set; }
    public bool Estado { get; set; }
}
