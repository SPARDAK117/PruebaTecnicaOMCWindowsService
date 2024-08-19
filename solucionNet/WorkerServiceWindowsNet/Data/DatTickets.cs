using Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public class DatTickets
    {

        private readonly string _connectionString;

        // Constructor que acepta una cadena de conexión
        public DatTickets(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InsertarTicketAsync(Tickets ticket)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("InsertarTicket", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    // Asignar los parámetros de la stored procedure
                    command.Parameters.AddWithValue("@Id_Tienda", ticket.Id_Tienda);
                    command.Parameters.AddWithValue("@Id_Registradora", ticket.Id_Registradora);
                    command.Parameters.AddWithValue("@FechaHora", ticket.FechaHora);
                    command.Parameters.AddWithValue("@Ticket", ticket.Ticket);
                    command.Parameters.AddWithValue("@Impuesto", ticket.Impuesto);
                    command.Parameters.AddWithValue("@Total", ticket.Total);
                    command.Parameters.AddWithValue("@FechaHora_Creacion", ticket.FechaHora_Creacion);

                    // Ejecutar la stored procedure
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
    