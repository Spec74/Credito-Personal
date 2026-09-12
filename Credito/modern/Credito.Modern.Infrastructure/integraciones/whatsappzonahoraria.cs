namespace Credito.Modern.Infrastructure.Integraciones;

internal static class WhatsAppZonaHoraria
{
    public static TimeZoneInfo Resolver(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            var fallback = OperatingSystem.IsWindows()
                ? "SA Pacific Standard Time"
                : "America/Lima";
            return TimeZoneInfo.FindSystemTimeZoneById(fallback);
        }
    }
}
