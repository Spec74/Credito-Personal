using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.CajaMaestro;

public sealed record GuardarCajaRequest(
    int CajaId,
    int OficinaId,
    string Denominacion,
    int? CajeroId,
    bool Estado);

public sealed record CajaGestionListItemDto(
    int CajaId,
    string Denominacion,
    int OficinaId,
    string? OficinaDenominacion,
    int? CajeroId,
    string? CajeroNombre,
    bool Estado,
    bool IndAbierto);

public sealed record CajaGestionPageDto(
    int Page,
    int PageSize,
    int TotalRecords,
    int TotalPages,
    List<CajaGestionListItemDto> Rows);

public sealed record CajaGestorItemDto(int UsuarioId, string NombreCompleto);

public sealed record CajaComboItemDto(int CajaId, string Denominacion);
