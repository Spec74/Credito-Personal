namespace Credito.Modern.Application.Prendario;

/// <summary>Hora de la pasada diaria del aviso prendario (08:00 Lima por defecto).</summary>
public static class PrendarioWhatsAppProgramacion
{
    public static DateTimeOffset ProximaCorrida(DateTimeOffset ahoraEnZona, int horaLocal)
    {
        var hora = Math.Clamp(horaLocal, 0, 23);
        var proxima = new DateTimeOffset(
            ahoraEnZona.Year,
            ahoraEnZona.Month,
            ahoraEnZona.Day,
            hora,
            0,
            0,
            ahoraEnZona.Offset);
        if (proxima <= ahoraEnZona)
        {
            proxima = proxima.AddDays(1);
        }

        return proxima;
    }
}
