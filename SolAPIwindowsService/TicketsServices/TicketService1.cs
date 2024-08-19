using DatTickets;
using System.Data.SqlClient;

namespace webapi.Servicio
{
    public class TicketService1
    {
        string cadenaConexion = "Server=34.218.6.36,1435\\evaluaciones;Database=evaluacion_omejia;User Id=sa;Password=abcd1234;";


        public string TicketInsert(string IdTienda, string IdRegistradora, DateTime Fecha, int NumTicket
            , float impuesto, float total)
        {
            List<dataTickets> ultimos = new List<dataTickets>();
            using (SqlConnection conn = new SqlConnection(cadenaConexion))
            {

                try
                {
                    conn.Open();
                    System.Data.Common.DbDataReader r = null;
                    SqlCommand cmd = new SqlCommand("InsertarTicket", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Id_Tienda", IdTienda);
                    cmd.Parameters.AddWithValue("@Id_Registradora", IdRegistradora);
                    cmd.Parameters.AddWithValue("@FechaHora", Fecha);
                    cmd.Parameters.AddWithValue("@Ticket", NumTicket);
                    cmd.Parameters.AddWithValue("@Impuesto", impuesto);
                    cmd.Parameters.AddWithValue("@Total", total);
                    r = cmd.ExecuteReader();

                }
                catch (Exception ex)
                {
                    return "Error";
                }


            }
            return "";
        }
    }



}
