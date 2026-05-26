using System.Collections.Generic;
using ITB.VENDIX.DA;
using ITB.VENDIX.BL;

namespace VendixWeb.Models
{
    public class ReporteConstanciaAlmacen
    {
        public MovimientoBL.EntradaSalida Cabecera { get; set; }
        public List<MovimientoDet> Detalle { get; set; }
     }
}