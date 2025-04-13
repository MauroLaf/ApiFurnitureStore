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
    public class ProductsController : ControllerBase
    {

        private readonly ApiFurnitureStoreContext _context;
        public ProductsController(ApiFurnitureStoreContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IEnumerable<Product>> GetAllProduct()
        {
            return await _context.Products.ToListAsync();
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetailsProduct(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id); //le digo que product va a contener lo que en la tabla tenga id igual al id que se pasa por parametro
            if (product == null) return NotFound();
            return Ok(product);     
        }
        [HttpGet("GetByCategory/{productCategoryId}")]
        public async Task<IEnumerable<Product>> GetByCategory(int productCategoryId)
        {
            //usare linq para consultas
            return await _context.Products
                                   .Where (p => p.ProductCategoryId == productCategoryId)
                                   .ToListAsync();
        }
        [HttpPost]
        public async Task<IActionResult> PostProduct(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync(); // no olvidar guardar cambios
            return CreatedAtAction("PostProduct", product.Id, product);
        }
        [HttpPut]
        public async Task<IActionResult> PutProduct(Product product)
        {
            _context.Products.Update(product); //no debo poner async porque solo marco el objeto como modified no accedo a la dbset
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteProduct(Product product)
        {
            if(product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
