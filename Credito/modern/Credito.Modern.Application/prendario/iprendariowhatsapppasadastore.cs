namespace Credito.Modern.Application.Prendario;

public interface IPrendarioWhatsAppPasadaStore
{
    PrendarioWhatsAppPasadaDto? Ultima { get; }

    void Registrar(PrendarioWhatsAppPasadaDto pasada);
}
