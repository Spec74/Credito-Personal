namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Variantes de presentación del PDF de créditos vencidos.</summary>
public enum RptCreditoVencidoPdfMode
{
    /// <summary>Informe estándar de créditos vencidos.</summary>
    Estandar = 0,

    /// <summary>Listado para notificar vencimientos (clientes que no pagan).</summary>
    Notificaciones = 1,
}
