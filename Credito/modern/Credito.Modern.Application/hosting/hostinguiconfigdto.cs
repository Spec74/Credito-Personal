namespace Credito.Modern.Application.Hosting;

public sealed record HostingUiConfigDto(
    bool UseSpaForModule,
    bool DefaultLoginToSpa,
    string SpaBasePath,
    int ObservacionDias,
    string Fase);
