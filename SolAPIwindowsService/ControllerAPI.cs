using Microsoft.AspNetCore.Mvc;
using TicketsServices;

namespace ControllerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly TicketService1 _ticketService;

        public TicketsController(TicketService1 ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpPost]
        public IActionResult InsertarTicket([FromBody] dataTickets ticket)
        {
            if (ticket == null)
            {
                return BadRequest("El ticket no puede ser nulo.");
            }

            _ticketService.InsertarTicket(ticket);

            return Ok("Ticket insertado correctamente.");
        }
    }
}
