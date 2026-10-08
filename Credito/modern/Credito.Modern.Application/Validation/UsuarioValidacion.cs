using Credito.Modern.Application.UsuariosAdmin;

namespace Credito.Modern.Application.Validation;

/// <summary>Alta/edición de usuarios admin (paridad + reglas 2026).</summary>
public static class UsuarioValidacion
{
    public const int MaxClave = 100;
    public const int MinClaveNueva = 8;

    public static string? ValidarClaveNueva(string? clave, string fieldName = "clave")
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            return $"{fieldName} es obligatoria.";
        }

        var v = clave.Trim();
        if (v.Length < MinClaveNueva)
        {
            return $"{fieldName} debe tener al menos {MinClaveNueva} caracteres.";
        }

        if (v.Length > MaxClave)
        {
            return $"{fieldName}: máximo {MaxClave} caracteres.";
        }

        if (!v.Any(char.IsLetter) || !v.Any(char.IsDigit))
        {
            return $"{fieldName} debe incluir al menos una letra y un número.";
        }

        return null;
    }

    public static string? ValidarGuardar(GuardarUsuarioRequest request, DateTime? hoy = null)
    {
        var esNuevo = request.UsuarioId < 1;
        var clavePlaceholder = string.Equals(request.ClaveUsuario, "********", StringComparison.Ordinal);
        var today = (hoy ?? DateTime.Today).Date;
        var claveNueva = !clavePlaceholder && !string.IsNullOrWhiteSpace(request.ClaveUsuario);

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
            (esNuevo || claveNueva) && !clavePlaceholder
                ? ValidarClaveNueva(request.ClaveUsuario)
                : null);
    }

    public static string? ValidarAsignarRoles(int oficinaId, int[]? rolIds)
    {
        if (oficinaId < 1)
        {
            return "oficinaId debe ser >= 1.";
        }

        if (rolIds is null || rolIds.Count(id => id >= 1) < 1)
        {
            return "Debe asignar al menos un rol a la oficina.";
        }

        return null;
    }
}
