using Credito.Modern.Application.CreditoTareas;

namespace Credito.Modern.Tests;

public class TareaReporteBuilderTests
{
    [Fact]
    public void ConstruirFilasOrdenadas_ordena_por_nombre_cliente()
    {
        var tareas = new List<TareaListItemDto>
        {
            CrearTarea(2, "Zorro", "11111111", 100),
            CrearTarea(1, "Ana", "22222222", 200),
            CrearTarea(3, "Ana", "22222222", 150),
        };
        var subs = new Dictionary<int, IReadOnlyList<SubtareaDto>>();

        var filas = TareaReporteBuilder.ConstruirFilasOrdenadas(tareas, subs, "PEN");

        Assert.Equal(3, filas.Count);
        Assert.Equal("Ana", filas[0].ClienteNombre);
        Assert.Equal("Ana", filas[1].ClienteNombre);
        Assert.Equal(150, filas[1].CreditoId);
        Assert.Equal("Zorro", filas[2].ClienteNombre);
        Assert.Equal(1, filas[0].Nro);
        Assert.Equal(3, filas[2].Nro);
    }

    [Fact]
    public void AgruparPorCliente_reune_varias_tareas_mismo_nombre()
    {
        var filas = new List<TareaReporteRowDto>
        {
            new()
            {
                Nro = 1,
                ClienteNombre = "Ana",
                ClienteDni = "111",
                Cliente = "111 - Ana",
                CreditoId = 10,
                TareaId = 1,
            },
            new()
            {
                Nro = 2,
                ClienteNombre = "Ana",
                ClienteDni = "111",
                Cliente = "111 - Ana",
                CreditoId = 20,
                TareaId = 2,
            },
            new()
            {
                Nro = 3,
                ClienteNombre = "Bruno",
                ClienteDni = "222",
                Cliente = "222 - Bruno",
                CreditoId = 30,
                TareaId = 3,
            },
        };

        var grupos = TareaReporteBuilder.AgruparPorCliente(filas);

        Assert.Equal(2, grupos.Count);
        Assert.Equal("Ana", grupos[0].ClienteNombre);
        Assert.Equal(2, grupos[0].Tareas.Count);
        Assert.Equal("Bruno", grupos[1].ClienteNombre);
    }

    [Fact]
    public void ExtraerDesdeEntrada_incluye_dni_y_codigo_corchetes()
    {
        var terminos = TareaBusquedaTerminos.ExtraerDesdeEntrada("44684156 Juan Pérez [CC040]");
        Assert.Contains("44684156", terminos);
        Assert.Contains("CC040", terminos);
    }

    private static TareaListItemDto CrearTarea(
        int tareaId,
        string nombre,
        string dni,
        int creditoId) =>
        new(
            tareaId,
            creditoId,
            dni,
            nombre,
            1000m,
            "analista",
            DateTime.UtcNow,
            null,
            "PEN",
            0,
            0);
}
