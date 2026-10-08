using Credito.Modern.Application.UsuariosAdmin;

namespace Credito.Modern.Application.Validation;

/// <summary>Alta/edición de usuarios admin (paridad + reglas 2026).</summary>
public static class UsuarioValidacion
{
    public const int MaxClave = 100;
    public const int MinClaveNueva = 6;

    public static string? ValidarGuardar(GuardarUsuarioRequest request, DateTime? hoy = null)
    {
        var esNuevo = request.UsuarioId < 1;
        var clavePlaceholder = string.Equals(request.ClaveUsuario, "********", StringComparison.Ordinal);
        var today = (hoy ?? DateTime.Today).Date;

        return ValidationGate.First(
            StringRules.RequireDni(request.NumeroDocumento),
            StringRules.RequireText(request.ApePaterno, "apellido paterno", StringRules.MaxNombre),
            StringRules.RequireText(request.ApeMaterno, "apellido materno", StringRules.MaxNombre),
            StringRules.RequireText(request.Nombre, "nombres", StringRules.MaxNombre),
            StringRules.RequireText(request.NombreUsuario, "usuario", StringRules.MaxUsuario),
            DateRules.OptionalFechaNacimiento(request.FechaNacimiento, today),
            StringRules.OptionalCelularPeruano(request.TelefonoMovil),
            StringRules.OptionalEmail(request.EmailPersonal),
            StringRules.OptionalDireccionRealista(request.Direccion, "dirección"),
            esNuevo && string.IsNullOrWhiteSpace(request.ClaveUsuario)
                ? "La clave es obligatoria para un usuario nuevo."
                : null,
            !esNuevo && !clavePlaceholder && !string.IsNullOrWhiteSpace(request.ClaveUsuario)
                && request.ClaveUsuario.Trim().Length < MinClaveNueva
                ? $"La clave debe tener al menos {MinClaveNueva} caracteres."
                : null,
            esNuevo && !string.IsNullOrWhiteSpace(request.ClaveUsuario)
                && request.ClaveUsuario.Trim().Length < MinClaveNueva
                ? $"La clave debe tener al menos {MinClaveNueva} caracteres."
                : null,
            StringRules.OptionalMaxLength(request.ClaveUsuario, "clave", MaxClave));
    }
}
