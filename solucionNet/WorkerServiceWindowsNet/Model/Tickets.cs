namespace Model
{
    public class Tickets
    {
        public string Id_Tienda { get; set; }  // Columna "Id_Tienda"
        public string Id_Registradora { get; set; }  // Columna "Id_Registradora"
        public DateTime FechaHora { get; set; }  // Columna "FechaHora"
        public int Ticket { get; set; }  // Columna "Ticket"
        public decimal Impuesto { get; set; }  // Columna "Impuesto" de tipo money en SQL Server
        public decimal Total { get; set; }  // Columna "Total" de tipo money en SQL Server
        public DateTime FechaHora_Creacion { get; set; }  // Columna "FechaHora_Creacion"
    }
}
