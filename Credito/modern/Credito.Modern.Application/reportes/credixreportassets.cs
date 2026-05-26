namespace Credito.Modern.Application.Reportes;

/// <summary>Recursos embebidos compartidos (logo RDLC <c>logocredito</c>).</summary>
public static class CredixReportAssets
{
    private static byte[]? _logoBytes;

    public static byte[] LoadLogo()
    {
        if (_logoBytes is not null)
            return _logoBytes;

        var asm = typeof(CredixReportAssets).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("logocredito.jpg", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "No se encontró ReportAssets/logocredito.jpg como recurso embebido.");

        using var stream = asm.GetManifestResourceStream(name)!;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _logoBytes = ms.ToArray();
        return _logoBytes;
    }
}
