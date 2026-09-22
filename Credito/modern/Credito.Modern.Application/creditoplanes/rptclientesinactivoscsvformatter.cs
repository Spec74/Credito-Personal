using System.Globalization;
using System.Text;
using Credito.Modern.Application;

namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// CSV UTF-8 con BOM para Excel; mismos datos que <see cref="IRptClientesInactivosReadService"/>.
/// </summary>
public static class RptClientesInactivosCsvFormatter
{
    public static byte[] ToUtf8BomCsv(IReadOnlyList<RptClientesInactivosRowDto> rows)
    {
        var sb = new StringBuilder(capacity: Math.Max(256, rows.Count * 160));
        sb.AppendLine(
            "PersonaId,Agente,Codigo,Dni,Cliente,Direccion,DireccionRef,Celular,Calificacion," +
            "ClasificacionRiesgoSBS,Depurado,DireccionNegocio,DireccionNegocioRef," +
            "MontoCredito,TotalCreditos,FechaCancelacion,TopeCredito,DiasInactividad");
        var inv = CultureInfo.InvariantCulture;
        foreach (var r in rows)
        {
            sb.Append(r.PersonaId.ToString(inv)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Agente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Codigo)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Dni)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Cliente)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Direccion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DireccionRef)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Celular)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Calificacion)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.ClasificacionRiesgoSBS)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.Depurado)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DireccionNegocio)).Append(',')
                .Append(CsvUtf8BomEncoding.EscapeField(r.DireccionNegocioRef)).Append(',')
                .Append(r.MontoCredito.ToString(inv)).Append(',')
                .Append(r.TotalCreditos.ToString(inv)).Append(',')
                .Append(r.FechaCancelacion?.ToString("yyyy-MM-dd", inv) ?? string.Empty).Append(',')
                .Append(r.TopeCredito.ToString(inv)).Append(',')
                .Append(r.DiasInactividad.ToString(inv))
                .AppendLine();
        }

        return CsvUtf8BomEncoding.GetBytes(sb.ToString());
    }
}
