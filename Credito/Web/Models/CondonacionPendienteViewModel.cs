namespace Web.Models
{
    public class CondonacionPendienteViewModel
    {
        public int Id { get; set; }
        public int CreditoId { get; set; }
        public int PersonaId { get; set; }
        public string NombreCliente { get; set; }
        public string NombreUsuario { get; set; }
        public decimal MontoCredito { get; set; }
        public decimal MoraCondonacion { get; set; }
        public decimal TotalPago { get; set; }
    }
}
