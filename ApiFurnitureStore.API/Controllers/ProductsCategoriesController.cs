using ApiFurnitureStore.Data;
using ApiFurnitureStore.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiFurnitureStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsCategoriesController : ControllerBase
    {
        private readonly ApiFurnitureStoreContext _context;
        public ProductsCategoriesController(ApiFurnitureStoreContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task <IEnumerable<ProductCategory>> GetAllProdCategories()
        {
           return await _context.ProductCategories.ToListAsync();
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetailsProdCategory(int id)
        {
            var prodcategory = await _context.ProductCategories.FirstOrDefaultAsync(pc => pc.Id==id);
            if (prodcategory == null) return NotFound();
            return Ok(prodcategory);
        }
        [HttpPost]
        public async Task<IActionResult> PostProdCategories(ProductCategory prodcategory)
        {
            await _context.ProductCategories.AddAsync(prodcategory);
            await _context.SaveChangesAsync();
            return CreatedAtAction("PostProdCategories", prodcategory.Id, prodcategory);
        }
        [HttpPut]
        public async Task<IActionResult> PutProdCategories(ProductCategory prodcategory)
        {
            _context.ProductCategories.Update(prodcategory);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteProdCategories(ProductCategory productCategory)
        {
            if(productCategory == null) return NotFound();
            _context.ProductCategories.Remove(productCategory);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        
    }
}
