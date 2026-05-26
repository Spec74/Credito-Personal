namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fecha operativa del sistema (paridad <c>VendixGlobal.GetFecha()</c> en legado).</summary>
public interface IFechaOperativaReadService
{
    Task<DateTime> ObtenerFechaAsync(CancellationToken cancellationToken = default);
}
