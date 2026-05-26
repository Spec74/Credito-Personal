namespace Credito.Modern.Application.ValorTablas;

public interface IValorTablaReadService
{
    /// <summary>Filas de <c>MAESTRO.ValorTabla</c> para un <paramref name="tablaId"/>; si <paramref name="soloItemIdPositivo"/> es true, excluye filas con ItemId menor o igual a 0 (combos en MVC).</summary>
    Task<List<ValorTablaListItemDto>> GetByTablaIdAsync(int tablaId, bool soloItemIdPositivo, CancellationToken cancellationToken = default);
}
