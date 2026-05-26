namespace Credito.Modern.Application.Almacenes;

/// <summary>Primera fila devuelta por <c>ALMACEN.usp_ExisteSerieArticulo</c> (como <c>SerieArticuloBL.ValidarExisteSerie</c>).</summary>
public sealed record ExisteSerieArticuloResponse(string? Resultado);
