namespace Credito.Modern.Application.CreditoTareas;

public interface ITareasWriteService
{
    Task<GuardarTareaResponse> GuardarAsync(
        GuardarTareaRequest request,
        int usuarioId,
        int oficinaId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);

    Task<TareaOperacionResponse> EliminarAsync(
        int tareaId,
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default);

    Task<TareaOperacionResponse> CompletarAsync(
        int tareaId,
        bool completada,
        int usuarioId,
        int oficinaId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
