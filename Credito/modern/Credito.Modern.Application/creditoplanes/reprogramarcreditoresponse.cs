namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Resultado de <c>CREDITO.usp_ReprogramarCredito</c>.</summary>
public sealed record ReprogramarCreditoResponse(
    int CreditoId,
    int NuevoCreditoId,
    decimal Deuda,
    bool Ok = true);
