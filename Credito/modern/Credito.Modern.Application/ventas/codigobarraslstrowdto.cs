namespace Credito.Modern.Application.Ventas;

/// <summary>Filas devueltas por <c>VENTAS.usp_CodigoBarras_Lst</c> (etiquetas por movimiento).</summary>
public sealed record CodigoBarrasLstRowDto(
    string? Serie1,
    string? Articulo1,
    decimal Precio1,
    string? Serie2,
    string? Articulo2,
    decimal Precio2);
