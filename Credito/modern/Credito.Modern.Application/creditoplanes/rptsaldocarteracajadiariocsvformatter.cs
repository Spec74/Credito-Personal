using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptSaldoCarteraCajaDiarioReadService"/>.
/// </summary>
public static class RptSaldoCarteraCajaDiarioCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptSaldoCarteraCajaDiarioRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 200));
        sb.AppendLine(
            "AgenteId,Oficina,Caja,Agente,FechaCierreIni,SalidasIni,MontoCobradoIni,PocentajeCobroIni,SaldoCarteraSinMoraIni,NroClientesCarteraSinMoraIni,SaldoMoraCarteraIni,NroClientesSaldoMoraCarteraIni,NroClientesNuevosIni,SaldoVencidoIni,SaldoMorosidadIni,FechaCierreFin,SalidasFin,MontoCobradoFin,PocentajeCobroFin,SaldoCarteraSinMoraFin,NroClientesCarteraSinMoraFin,SaldoMoraCarteraFin,NroClientesSaldoMoraCarteraFin,NroClientesNuevosFin,SaldoVencidoFin,SaldoMorosidadFin");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.AgenteId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Oficina)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Caja)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(
                    r.FechaCierreIni is null
                        ? string.Empty
                        : r.FechaCierreIni.Value.ToString(DateTimeFormat, inv))
                .Append(',')
                .Append(r.SalidasIni.ToString(inv)).Append(',')
                .Append(r.MontoCobradoIni?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.PocentajeCobroIni?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SaldoCarteraSinMoraIni.ToString(inv)).Append(',')
                .Append(r.NroClientesCarteraSinMoraIni.ToString(inv)).Append(',')
                .Append(r.SaldoMoraCarteraIni.ToString(inv)).Append(',')
                .Append(r.NroClientesSaldoMoraCarteraIni.ToString(inv)).Append(',')
                .Append(r.NroClientesNuevosIni.ToString(inv)).Append(',')
                .Append(r.SaldoVencidoIni.ToString(inv)).Append(',')
                .Append(r.SaldoMorosidadIni.ToString(inv)).Append(',')
                .Append(
                    r.FechaCierreFin is null
                        ? string.Empty
                        : r.FechaCierreFin.Value.ToString(DateTimeFormat, inv))
                .Append(',')
                .Append(r.SalidasFin.ToString(inv)).Append(',')
                .Append(r.MontoCobradoFin.ToString(inv)).Append(',')
                .Append(r.PocentajeCobroFin?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SaldoCarteraSinMoraFin.ToString(inv)).Append(',')
                .Append(r.NroClientesCarteraSinMoraFin.ToString(inv)).Append(',')
                .Append(r.SaldoMoraCarteraFin.ToString(inv)).Append(',')
                .Append(r.NroClientesSaldoMoraCarteraFin.ToString(inv)).Append(',')
                .Append(r.NroClientesNuevosFin.ToString(inv)).Append(',')
                .Append(r.SaldoVencidoFin.ToString(inv)).Append(',')
                .Append(r.SaldoMorosidadFin.ToString(inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
