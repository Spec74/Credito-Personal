using Credito.Modern.Application.Articulos;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Application.Validation;

/// <summary>Validación de artículos y gestión de crédito (cargos / observaciones).</summary>
public static class CreditoProductoValidacion
{
    public static string? ValidarGuardarArticulo(GuardarArticuloRequest request) =>
        ValidationGate.First(
            IdRules.RequirePositive(request.ModeloId, "modeloId"),
            IdRules.RequirePositive(request.TipoArticuloId, "tipoArticuloId"),
            MaestroValidacion.ValidarDenominacion(request.Denominacion),
            StringRules.OptionalMaxLength(request.Descripcion, "descripcion", StringRules.MaxGlosa),
            StringRules.OptionalMaxLength(request.CodArticulo, "codArticulo", StringRules.MaxSerie),
            MoneyRules.RequirePositive(request.Monto, "monto"),
            MoneyRules.RequireNonNegative(request.Descuento, "descuento"));

    public static string? ValidarGuardarCargo(GuardarCargoCreditoRequest request) =>
        ValidationGate.First(
            IdRules.RequirePositive(request.OficinaId, "oficinaId"),
            IdRules.RequirePositive(request.CreditoId, "creditoId"),
            IdRules.RequirePositive(request.TipoCargoId, "tipoCargoId"),
            MoneyRules.RequirePositive(request.Monto, "monto"),
            StringRules.RequireText(request.Descripcion, "descripcion", StringRules.MaxGlosa));

    public static string? ValidarObservacion(string? observacion) =>
        StringRules.RequireText(observacion, "observacion", StringRules.MaxObservacion);
}
