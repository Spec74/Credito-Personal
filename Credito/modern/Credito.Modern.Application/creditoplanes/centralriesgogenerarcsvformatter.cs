using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="ICentralRiesgoGenerarReadService"/>.
/// </summary>
public static class CentralRiesgoGenerarCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<CentralRiesgoGenerarRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(512, rows.Count * 128));
        sb.AppendLine(
            "Anio,Mes,CreditoId,Periodo,Entidad,TipoDoc,NumDoc,RazonSocial,ApePat,ApeMat,Nombres,TipoPersona,ModalidadCredito,DeudaMenor30,DeudaMayor30,Calificacion,DiasAtrazo,Direccion,celular");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.Anio?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.Mes?.ToString(inv) ?? string.Empty).Append(',')
                .Append(r.CreditoId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Periodo)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Entidad)).Append(',')
                .Append(r.TipoDoc.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.NumDoc)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.RazonSocial)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.ApePat)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.ApeMat)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Nombres)).Append(',')
                .Append(r.TipoPersona.ToString(inv)).Append(',')
                .Append(r.ModalidadCredito.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DeudaMenor30)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DeudaMayor30)).Append(',')
                .Append(r.Calificacion.ToString(inv)).Append(',')
                .Append(r.DiasAtrazo.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Direccion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.celular))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
