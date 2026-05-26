namespace Credito.Modern.Application.Modelos;

public sealed record ModeloListItemDto(int ModeloId, string Denominacion, int? MarcaId, bool Estado);
