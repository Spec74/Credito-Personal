namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Entrada del catálogo de pantallas/acciones de informes expuestas por <c>ReporteController</c> (MVC).
/// Sirve para BFF/SPA antes de sustituir export PDF RDLC.
/// </summary>
public sealed record ReporteCatalogoItemDto(string Id, string Nombre, string Area, string AccionMvc);
