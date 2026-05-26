namespace Credito.Modern.Application.Clientes;

/// <summary>Fila de grilla (paridad <c>Clientejgrid</c> / jqGrid Index).</summary>
public sealed record ClienteListadoRowDto(
    int PersonaId,
    string Codigo,
    string Cliente,
    string Documento,
    string? Celular,
    string? Email,
    string? Direccion);

public sealed record ClienteListadoResultDto(
    IReadOnlyList<ClienteListadoRowDto> Rows,
    int Total,
    int Page,
    int PageSize);

/// <summary>Persona por DNI/RUC para precargar alta (paridad ObtenerClienteDNI).</summary>
public sealed record PersonaPorDocumentoDto(
    int PersonaId,
    bool TieneCliente,
    string TipoPersona,
    string Nombre,
    string? ApePaterno,
    string? ApeMaterno,
    string NumeroDocumento,
    string Sexo,
    string? Email,
    string? Celular1,
    string? Direccion,
    DateTime? FechaNacimiento);
