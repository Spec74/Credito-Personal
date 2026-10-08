using Credito.Modern.Application.Clientes;

namespace Credito.Modern.Application.Validation;

/// <summary>Validación de alta/edición de cliente (paridad Mantener + reglas 2026).</summary>
public static class ClienteValidacion
{
    public static string? ValidarGuardar(GuardarClienteRequest request, DateTime? hoy = null)
    {
        var today = (hoy ?? DateTime.Today).Date;
        return ValidationGate.First(
            StringRules.RequireDocumento(request.NumeroDocumento, request.TipoPersona),
            StringRules.RequireText(request.Nombre, "nombre", StringRules.MaxNombre),
            request.TipoPersona is not ("N" or "J")
                ? "tipoPersona debe ser N o J."
                : null,
            request.TipoPersona == "N"
                ? ValidationGate.First(
                    StringRules.RequireText(request.ApePaterno, "apellido paterno", StringRules.MaxNombre),
                    StringRules.RequireText(request.ApeMaterno, "apellido materno", StringRules.MaxNombre),
                    DateRules.RequireFechaNacimiento(request.FechaNacimiento, today))
                : null,
            request.EstadoCivilId is 2 or 3 && request.ConyuguePersonaId is null or < 1
                ? "Cónyuge obligatorio para estado civil casado/conviviente."
                : null,
            StringRules.OptionalCelularPeruano(request.Celular1),
            StringRules.OptionalEmail(request.Email),
            StringRules.OptionalDireccionRealista(request.Direccion, "domicilio"),
            StringRules.OptionalMaxLength(request.DireccionRef, "referencia domicilio", StringRules.MaxDireccion),
            StringRules.OptionalDireccionRealista(request.DireccionNegocio, "dirección de negocio"),
            StringRules.OptionalMaxLength(request.DireccionNegocioRef, "referencia negocio", StringRules.MaxDireccion));
    }

    public static string? ValidarPersonaRapida(
        string? dni,
        string? nombre,
        string? apePaterno,
        string? apeMaterno,
        string? celular)
    {
        return ValidationGate.First(
            StringRules.RequireDni(dni),
            StringRules.RequireText(nombre, "nombres", StringRules.MaxNombre),
            StringRules.RequireText(apePaterno, "apellido paterno", StringRules.MaxNombre),
            StringRules.RequireText(apeMaterno, "apellido materno", StringRules.MaxNombre),
            StringRules.OptionalCelularPeruano(celular));
    }
}
