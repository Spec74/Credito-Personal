namespace Credito.Modern.Application.Almacenes;

/// <summary>Primera fila devuelta por <c>ALMACEN.usp_ListarSerieKardex</c> (como <c>AlmacenBL.ObtenerSerieKardex</c>).</summary>
public sealed record ListarSerieKardexResponse(string? Texto);
