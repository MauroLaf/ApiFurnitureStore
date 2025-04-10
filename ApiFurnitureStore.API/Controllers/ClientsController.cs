using ApiFurnitureStore.Data;
using ApiFurnitureStore.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiFurnitureStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController] //decorador, definicion del endpoint
    public class ClientsController : ControllerBase
    {
        private readonly ApiFurnitureStoreContext _context;
        //constructor/inyeccion de dependencia
        public ClientsController(ApiFurnitureStoreContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IEnumerable<Client>> GetAllClient()
        {
            return await _context.Clients.ToListAsync();
        }

        [HttpGet("{id}")]
        //aca usare IActionResult para usar respuestas http response
        //antes puse Task<Client>
        public async Task<IActionResult> GetDetailsClient(int id)
        {
            //si pongo singleordefaul da error si no encuentra en cambio frist no arroja error sino null
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return NotFound(); //es mejor devolver una respuesta http
            return Ok(client);
        }
        [HttpPost]
        public async Task <IActionResult> PostClient(Client client)
        {
            await _context.Clients.AddAsync(client);
            await _context.SaveChangesAsync();
            //aca ponemos un createdatAction porque usamos action result y le damos el nombre del metodo donde se creo 
            return CreatedAtAction("PostClient", client.Id, client);
        }
        [HttpPut]
        public async Task<IActionResult> PutClient(Client client)
        {
            _context.Clients.Update(client);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete]
        public async Task <IActionResult> DeleteClient(Client client)
        {
            if (client == null) return NotFound();  
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return NoContent(); //cuando no hay nada que retornar se pone esto da 204 ok
        }
    }
}
