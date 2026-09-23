namespace Credito.Modern.Application.CreditoPlanes;

public interface ISaldosCierreWriteService
{
    /// <summary>
    /// Paridad <c>CajaDiarioBL.ActualizarDatosPostCierreBoveda</c>:
    /// saldo cartera + calificar + intento de cierre gerencial mensual
    /// (<c>usp_IntentarGenerarCierreGerencialMensual</c>; solo actúa fin de mes / días 1–2).
    /// </summary>
    Task ActualizarDatosPostCierreBovedaAsync(
        int oficinaId,
        int usuarioCierreId,
        CancellationToken cancellationToken = default);
}
