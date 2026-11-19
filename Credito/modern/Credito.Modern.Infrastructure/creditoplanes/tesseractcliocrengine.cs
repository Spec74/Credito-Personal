using System.Diagnostics;
using System.Text;
using Credito.Modern.Application.CreditoPlanes;
using Microsoft.Extensions.Logging;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

/// <summary>
/// OCR vía CLI <c>tesseract</c> (Docker: tesseract-ocr + spa). Sin tesseract → IsAvailable=false.
/// </summary>
public sealed class TesseractCliOcrEngine(ILogger<TesseractCliOcrEngine> logger) : IOcrEngine
{
    private static readonly Lazy<bool> Available = new(Detect);

    public bool IsAvailable => Available.Value;

    public async Task<string> ExtractTextAsync(
        Stream image,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "OCR no disponible: instale Tesseract (tesseract-ocr + idioma spa) en el servidor.");
        }

        ArgumentNullException.ThrowIfNull(image);
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
        {
            ext = ".png";
        }

        var tmpDir = Path.Combine(Path.GetTempPath(), "credito-ocr");
        Directory.CreateDirectory(tmpDir);
        var imgPath = Path.Combine(tmpDir, $"{Guid.NewGuid():N}{ext}");
        var outBase = Path.Combine(tmpDir, Guid.NewGuid().ToString("N"));

        try
        {
            await using (var fs = File.Create(imgPath))
            {
                await image.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            }

            var psi = new ProcessStartInfo
            {
                FileName = ResolveTesseractBin(),
                ArgumentList = { imgPath, outBase, "-l", "spa+eng", "--psm", "6" },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("No se pudo iniciar tesseract.");

            var stderrTask = proc.StandardError.ReadToEndAsync(cancellationToken);
            await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            if (proc.ExitCode != 0)
            {
                logger.LogWarning("tesseract exit {Code}: {Err}", proc.ExitCode, stderr);
                throw new InvalidOperationException("No se pudo leer la captura con OCR.");
            }

            var txtPath = outBase + ".txt";
            if (!File.Exists(txtPath))
            {
                return string.Empty;
            }

            return await File.ReadAllTextAsync(txtPath, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            TryDelete(imgPath);
            TryDelete(outBase + ".txt");
        }
    }

    private static bool Detect()
    {
        try
        {
            var bin = ResolveTesseractBin();
            var psi = new ProcessStartInfo
            {
                FileName = bin,
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit(4000);
            return p.ExitCode == 0 || p.ExitCode == 1; // some builds return 1 on --version quirks
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveTesseractBin()
    {
        var env = Environment.GetEnvironmentVariable("TESSERACT_PATH");
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();
        return OperatingSystem.IsWindows() ? "tesseract.exe" : "tesseract";
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // ignore temp cleanup
        }
    }
}
