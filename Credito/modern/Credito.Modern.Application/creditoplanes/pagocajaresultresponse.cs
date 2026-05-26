namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Identificador devuelto por procs de pago (<c>Nullable&lt;int&gt;</c> en EF); el MVC trata &lt; 0 como error.</summary>
public sealed record PagoCajaResultResponse(int? ResultId);
