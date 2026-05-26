namespace Credito.Modern.Application.Maestros;

public interface IMarcaWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(GuardarMarcaRequest request, CancellationToken ct = default);
    Task<MaestroOperacionResponse> ActivarAsync(int marcaId, CancellationToken ct = default);
}

public interface IModeloWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(GuardarModeloRequest request, CancellationToken ct = default);
    Task<MaestroOperacionResponse> ActivarAsync(int modeloId, CancellationToken ct = default);
}

public interface ITipoArticuloWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(GuardarTipoArticuloRequest request, CancellationToken ct = default);
    Task<MaestroOperacionResponse> ActivarAsync(int tipoArticuloId, CancellationToken ct = default);
}

public interface IOficinaWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(GuardarOficinaRequest request, CancellationToken ct = default);
    Task<MaestroOperacionResponse> ActivarAsync(int oficinaId, CancellationToken ct = default);
}

public interface IAlmacenWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(GuardarAlmacenRequest request, CancellationToken ct = default);
    Task<MaestroOperacionResponse> ActivarAsync(int almacenId, CancellationToken ct = default);
}

public interface IListaPrecioWriteService
{
    Task<MaestroOperacionResponse> GuardarAsync(GuardarListaPrecioRequest request, CancellationToken ct = default);
    Task<MaestroOperacionResponse> ActivarAsync(int listaPrecioId, CancellationToken ct = default);
}
