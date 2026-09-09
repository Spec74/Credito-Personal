namespace Credito.Modern.Infrastructure.Integraciones;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public bool Enabled { get; set; }

    public string Token { get; set; } = string.Empty;

    public string PhoneNumberId { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "v21.0";

    public string TemplateVencimientoPrendario { get; set; } = "aviso_vencimiento_prendario";

    public string TemplateLang { get; set; } = "es";

    public int DiasAntes { get; set; } = 3;

    /// <summary>Hora local (0-23) del envío diario automático.</summary>
    public int DailyHourLocal { get; set; } = 8;

    public string TimeZoneId { get; set; } = "SA Pacific Standard Time";

    /// <summary>Si es true, corre una pasada al arrancar la API (sin esperar a las 08:00).</summary>
    public bool RunOnStartupIfPending { get; set; }

    public bool EstaConfigurado =>
        Enabled
        && !string.IsNullOrWhiteSpace(Token)
        && !string.IsNullOrWhiteSpace(PhoneNumberId);
}
