namespace Credito.Modern.Application.Menus;

public interface IMenuReadService
{
    /// <summary>Obtiene el menú dinámico por oficina y usuario (proc legado <c>MAESTRO.usp_MenuLst</c>).</summary>
    Task<List<MenuItemDto>> GetMenuAsync(int oficinaId, int usuarioId, CancellationToken cancellationToken = default);
}
