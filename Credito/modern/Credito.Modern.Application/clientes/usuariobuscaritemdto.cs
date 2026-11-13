namespace Credito.Modern.Application.Clientes;

/// <summary>
/// Autocomplete de usuarios activos. Incluye <see cref="UsuarioId"/> (para bóveda temporal / roles)
/// y <see cref="PersonaId"/> (compatibilidad con pantallas que buscan persona).
/// </summary>
public sealed record UsuarioBuscarItemDto(int UsuarioId, int PersonaId, string Label);
