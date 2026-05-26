using System.Linq;
using ITB.VENDIX.DA;

namespace ITB.VENDIX.BL
{
    public class MovimientoCajaChicaBL: Repositorio<MovimientoCajaChica>
    {
        public static MovCajaBase RptMovCajaOtros(int pMovimientoCajaChicaId)
        {
            using (var db = new VENDIXEntities())
            {
                var qrycre = from mc in db.MovimientoCajaChica
                             where mc.Id == pMovimientoCajaChicaId
                             select new MovCajaBase
                             {
                                 MovimientoCajaId = mc.Id,
                                 PersonaId = mc.PersonaId,
                                 Cliente = mc.Persona.NombreCompleto,
                                 User = mc.Usuario.NombreUsuario,
                                 FechaReg = mc.FechaReg,
                                 Oficina = "PRINCIPAL",
                                 Producto = "CAJA DIARIO",
                                 ImportePago = mc.Importe,
                                 Articulo = mc.Descripcion,
                                 IndEntrada = mc.IndEntrada
                             };
                return qrycre.ToList()[0];
            }
        }
    }
}
