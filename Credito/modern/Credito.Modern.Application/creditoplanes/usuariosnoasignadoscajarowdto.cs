namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_UsuariosNoAsignadosCaja</c> (paridad con <c>usp_UsuariosNoAsignadosCaja_Result</c> en DA).</summary>
public sealed class UsuariosNoAsignadosCajaRowDto
{
    public int Id { get; init; }

    public string? Valor { get; init; }
}
