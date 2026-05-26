namespace Credito.Modern.Application.Auth;

/// <summary>Misma información que envía el legado en <c>Home/Autenticar</c> (usuario, clave, oficina y <c>tk</c>).</summary>
public sealed record LoginRequest(string? NombreUsuario, string? Clave, int OficinaId, string? ClienteAcceso);
