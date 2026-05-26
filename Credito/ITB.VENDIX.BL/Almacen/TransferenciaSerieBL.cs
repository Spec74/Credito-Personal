using ITB.VENDIX.DA;
using System;

namespace ITB.VENDIX.BL
{
    public class TransferenciaSerieBL : Repositorio<TransferenciaSerie>
    {
        public static void Eliminar(TransferenciaSerie transferenciaSerie)
        {
            throw new NotImplementedException();
        }


        public class EntradaSalida
        {
            public int TransferenciaSerieId { get; set; }
            public int TransferenciaId { get; set; }
            public string SerieArticuloId { get; set; }

        }
    }
}
