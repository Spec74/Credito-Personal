using Credito.Modern.Application.Maestros;

namespace Credito.Modern.Application.RolAdmin;

public sealed record GuardarRolRequest(int RolId, string Denominacion, bool Estado);

public sealed record RolGestionListItemDto(int RolId, string Denominacion, bool Estado);

public sealed record RolDetalleDto(int RolId, string Denominacion, bool Estado);

public sealed record MenuAsignacionDto(int MenuId, string Denominacion, bool Asignado);

public sealed record RolMenusDetalleDto(RolDetalleDto Rol, List<MenuAsignacionDto> Menus);

public sealed record AsignarRolMenusRequest(int[] MenuIds);
