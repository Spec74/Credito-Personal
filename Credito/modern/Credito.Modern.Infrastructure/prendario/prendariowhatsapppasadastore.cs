using Credito.Modern.Application.Prendario;

namespace Credito.Modern.Infrastructure.Prendario;

public sealed class PrendarioWhatsAppPasadaStore : IPrendarioWhatsAppPasadaStore
{
    private readonly object _lock = new();
    private PrendarioWhatsAppPasadaDto? _ultima;

    public PrendarioWhatsAppPasadaDto? Ultima
    {
        get
        {
            lock (_lock)
            {
                return _ultima;
            }
        }
    }

    public void Registrar(PrendarioWhatsAppPasadaDto pasada)
    {
        lock (_lock)
        {
            _ultima = pasada;
        }
    }
}
