using System;

namespace VendixWeb.Models.CierreGerencial
{
    public sealed class MetaGerencialDefinitivaDto
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public DateTime Periodo { get; set; }

        public decimal CapitalBase { get; set; }
        public int ClientesActivosBase { get; set; }
        public decimal? VencidosBaseComparable { get; set; }
        public decimal VencidosBaseLegacy { get; set; }
        public int? ClientesVencidosBase { get; set; }

        public decimal? MetaCapitalCierre { get; set; }
        public int? MetaClientesActivosCierre { get; set; }
        public decimal? MetaVencidosMaximoCierre { get; set; }
        public decimal? MetaRecuperacionVencidosMes { get; set; }

        public bool Configurada { get; set; }
        public bool PeriodoCerrado { get; set; }
        public bool PuedeEditar { get; set; }
        public DateTime FechaLimiteEdicion { get; set; }

        public string Asesor { get; set; }
        public string Supervisor { get; set; }
        public string Mercado { get; set; }
        public short? Orden { get; set; }
        public string TipoCartera { get; set; }
    }

    public sealed class AvanceMetaGerencialDto
    {
        public short? Orden { get; set; }
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public string Asesor { get; set; }
        public string Supervisor { get; set; }
        public string Mercado { get; set; }
        public string TipoCartera { get; set; }
        public DateTime Periodo { get; set; }

        public decimal? CapitalBase { get; set; }
        public decimal? MetaCapitalCierre { get; set; }
        public decimal CapitalActual { get; set; }
        public decimal? DiferenciaCapital { get; set; }
        public decimal? CumplimientoCapitalPct { get; set; }
        public string EstadoCapital { get; set; }

        public int? ClientesBase { get; set; }
        public int? MetaClientesActivosCierre { get; set; }
        public int ClientesActivosActual { get; set; }
        public int? DiferenciaClientes { get; set; }
        public decimal? CumplimientoClientesPct { get; set; }
        public string EstadoClientes { get; set; }

        public decimal? VencidosBaseComparable { get; set; }
        public decimal? VencidosBaseLegacy { get; set; }
        public decimal? MetaVencidosMaximoCierre { get; set; }
        public decimal VencidosActual { get; set; }
        public decimal? MargenVencidos { get; set; }
        public string EstadoVencidos { get; set; }
        public int? ClientesVencidosBase { get; set; }
        public int ClientesVencidosActual { get; set; }
        public decimal ClientesVencidosPct { get; set; }

        public decimal? MetaRecuperacionVencidosMes { get; set; }
        public decimal? RecuperacionVencidosActual { get; set; }
        public string EstadoRecuperacion { get; set; }

        public bool MetaConfigurada { get; set; }
        public DateTime FechaCalculo { get; set; }
        public bool AvanceNoOficial { get; set; }
        public int ClientesNuevosMes { get; set; }
        public decimal MontoClientesNuevosMes { get; set; }
        public decimal MontoCobradoMes { get; set; }
        public decimal DesembolsosMes { get; set; }
        public int NroOperacionesMes { get; set; }
    }
}
