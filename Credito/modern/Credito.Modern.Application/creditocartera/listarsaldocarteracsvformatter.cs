using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoCartera;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IListarSaldoCarteraReadService"/>.
/// </summary>
public static class ListarSaldoCarteraCsvFormatter
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static byte[] ToUtf8BomCsv(IReadOnlyList<ListarSaldoCarteraRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 96));
        sb.AppendLine(
            "AgenteId,OficinaId,NroDesembolsos,MontoDesembolsos,SaldoCartera,NroClientesSaldoCartera,SaldoMoraCartera,NroClientesSaldoMoraCartera,SaldoVencido,SaldoMorosidad,NroClientesNuevos,FechaCierre");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.AgenteId.ToString(inv)).Append(',')
                .Append(r.OficinaId.ToString(inv)).Append(',')
                .Append(r.NroDesembolsos?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.MontoDesembolsos?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SaldoCartera?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.NroClientesSaldoCartera?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.SaldoMoraCartera.ToString(inv)).Append(',')
                .Append(r.NroClientesSaldoMoraCartera.ToString(inv)).Append(',')
                .Append(r.SaldoVencido.ToString(inv)).Append(',')
                .Append(r.SaldoMorosidad.ToString(inv)).Append(',')
                .Append(r.NroClientesNuevos.ToString(inv)).Append(',')
                .Append(
                    r.FechaCierre is null
                        ? string.Empty
                        : r.FechaCierre.Value.ToString(DateTimeFormat, inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
