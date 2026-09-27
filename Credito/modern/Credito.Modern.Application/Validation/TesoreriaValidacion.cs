namespace Credito.Modern.Application.Validation;

/// <summary>Operaciones de bóveda / tesorería (importes y glosas).</summary>
public static class TesoreriaValidacion
{
    public static string? ValidarMovimiento(decimal importe, string? glosa, string glosaField = "glosa") =>
        ValidationGate.First(
            MoneyRules.RequirePositive(importe, "importe"),
            StringRules.RequireText(glosa, glosaField, StringRules.MaxGlosa));

    public static string? ValidarMovimientoOficina(
        int oficinaId,
        decimal importe,
        string? descripcion,
        string descripcionField = "descripcion") =>
        ValidationGate.First(
            IdRules.RequirePositive(oficinaId, "oficinaId"),
            ValidarMovimiento(importe, descripcion, descripcionField));

    public static string? ValidarTransferenciaInterBoveda(
        int oficinaId,
        int bovedaInicioId,
        int bovedaDestinoId,
        decimal monto,
        string? glosa) =>
        ValidationGate.First(
            IdRules.RequirePositive(oficinaId, "oficinaId"),
            IdRules.RequirePositive(bovedaInicioId, "bovedaInicioId"),
            IdRules.RequirePositive(bovedaDestinoId, "bovedaDestinoId"),
            MoneyRules.RequirePositive(monto, "monto"),
            StringRules.RequireText(glosa, "glosa", StringRules.MaxGlosa));

    public static string? ValidarTransferenciaBancos(
        int oficinaId,
        int tipoPagoOrigenId,
        int tipoPagoDestinoId,
        decimal importe,
        string? glosa)
    {
        return ValidationGate.First(
            IdRules.RequirePositive(oficinaId, "oficinaId"),
            IdRules.RequirePositive(tipoPagoOrigenId, "tipoPagoOrigenId"),
            IdRules.RequirePositive(tipoPagoDestinoId, "tipoPagoDestinoId"),
            tipoPagoOrigenId == tipoPagoDestinoId
                ? "El banco de origen y el de destino no pueden ser iguales."
                : null,
            ValidarMovimiento(importe, glosa));
    }

    public static string? ValidarGuardarCaja(int oficinaId, string? denominacion) =>
        ValidationGate.First(
            IdRules.RequirePositive(oficinaId, "oficinaId"),
            MaestroValidacion.ValidarDenominacion(denominacion));

    public static string? ValidarEntradaSalidaCampos(
        decimal importe,
        string? descripcion,
        int tipoPagoId) =>
        ValidationGate.First(
            MoneyRules.RequirePositive(importe, "importe"),
            StringRules.RequireText(descripcion, "descripcion", StringRules.MaxGlosa),
            IdRules.RequirePositive(tipoPagoId, "tipoPagoId"));
}
