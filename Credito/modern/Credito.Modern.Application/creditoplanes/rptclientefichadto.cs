namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cabecera ficha cliente. Paridad parámetros RDLC <c>Reporte/ReporteCliente</c>
/// (<c>ClienteBL.Obtener</c> + tablas maestro + conteo créditos DES).
/// </summary>
public sealed record RptClienteFichaDto(
    int PersonaId,
    int CreditosDesembolsados,
    string Cliente,
    string NumeroDocumento,
    string? FechaNacimiento,
    string? Sexo,
    string? Direccion,
    string? DireccionRef,
    string? Celular,
    string Conyugue,
    string ConyugueDni,
    string ConyugueCelular,
    string TipoVivienda,
    string EstadoCivil,
    string Distrito,
    string ActividadEconomica,
    string? Nota,
    string? DireccionNegocio,
    string? DireccionNegocioRef);
