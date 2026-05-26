namespace Credito.Modern.Application.Integraciones;

public sealed record ApiPeruDniDto(
    bool Success,
    string? Nombres,
    string? ApellidoPaterno,
    string? ApellidoMaterno,
    string? Mensaje);

public sealed record ApiPeruRucDto(
    bool Success,
    string? RazonSocial,
    string? Direccion,
    string? Mensaje);
