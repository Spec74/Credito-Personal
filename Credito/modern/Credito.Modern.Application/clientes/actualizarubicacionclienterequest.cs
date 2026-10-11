namespace Credito.Modern.Application.Clientes;

/// <summary>Body de <c>POST /clientes/{personaId}/ubicacion</c>.</summary>
public sealed record ActualizarUbicacionClienteRequest(decimal Latitud, decimal Longitud);
