using ApiFurnitureStore.Data;
using ApiFurnitureStore.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiFurnitureStore.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {   
        private readonly ApiFurnitureStoreContext _context;
        public OrdersController(ApiFurnitureStoreContext context) 
        {
            _context = context;
        }
        [HttpGet] //como esta es la clase master le agregare include (es como el join en sql) para agregar detalles
        public async Task<IEnumerable<Order>> GetallOrder()
        {
            return await _context.Orders.Include(o => o.OrderDetails).ToListAsync();
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetailsOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return Ok(order);
        }
        [HttpPost]
        public async Task<IActionResult> PostOrder(Order order)
        {
            if (order.OrderDetails == null) //validamos
                return BadRequest("Order should have at least one details");
            await _context.Orders.AddAsync(order);//en esta linea inserto la orden
            await _context.OrderDetails.AddRangeAsync(order.OrderDetails);//inserto todos sus detalles con range y no iserto una a una con foreach. PROBE DEJAR VACIO y al cargar datos me pone id=0  no carga los detalles 
            await _context.SaveChangesAsync(); //nunca olvidar guardar los cambios por eso tambien no guardaba los cambios y el id era 0
            return CreatedAtAction("PostOrder", order.Id, order);
        }
        [HttpPut]
        public async Task<IActionResult> PutOrder(Order order)
        {
            //siempre tiene que tener una validacion, no puede ser null porque es un metodo de persistencia
            if (order == null) return NotFound();
            if (order.Id <= 0) return NotFound(); //el id tiene que estar para traer detalles

            var existingOrder = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == order.Id);
            if (existingOrder == null) return NotFound();

            //aca actualizo order "maestro"
            existingOrder.OrderNumber = order.OrderNumber;
            existingOrder.OrderDate = order.OrderDate;
            existingOrder.DeliveryDate = order.DeliveryDate;
            existingOrder.ClientId = order.ClientId;

            //para mantener integridad elimino las existentes en la base de datos
            _context.OrderDetails.RemoveRange(existingOrder.OrderDetails); //elimino todo junto con sus detalles
            _context.Orders.Update(existingOrder);//que actualice lo que tiene en bd al "maestro"
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(Order order)//esto tambien podria ir por id simplemente
        {
            if (order == null) return NotFound();

            var existingOrder = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == order.Id);
            if(existingOrder == null) return NotFound();

            _context.OrderDetails.RemoveRange(existingOrder.OrderDetails);
            _context.Orders.Remove(existingOrder);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
