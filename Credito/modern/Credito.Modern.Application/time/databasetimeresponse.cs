namespace Credito.Modern.Application.Time;

/// <summary>Respuesta JSON de la hora devuelta por SQL Server (<c>usp_FechaBD</c>).</summary>
public sealed record DatabaseTimeResponse(DateTime? DatabaseTime);
