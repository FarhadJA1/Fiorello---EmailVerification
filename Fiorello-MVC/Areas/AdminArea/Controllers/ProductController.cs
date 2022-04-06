using Fiorello_MVC.Data;
using Fiorello_MVC.Models;
using Fiorello_MVC.Utilities.Pagination;
using Fiorello_MVC.ViewModels.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fiorello_MVC.Areas.AdminArea.Controllers
{
    [Area("AdminArea")]
    public class ProductController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _context;
        public ProductController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        public async Task<IActionResult> Index(int page=1,int take=5)
        {
            var product = await _context.Products
                .Where(m=>!m.IsDelete)
                .Include(m => m.Category)
                .Include(m => m.Images)
                .OrderByDescending(m => m.Id)
                .Skip((page-1)*take)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
            var productsVM = GetMapDatas(product);

            int count = await GetPageCount(take);

            Pagination<ProductListVM> result = new Pagination<ProductListVM>(productsVM, page, count);

            return View(result);
        }

        private async Task<int> GetPageCount(int take)
        {
            var count = await _context.Products.CountAsync();
            return (int)Math.Ceiling((decimal)count / take);
        }
        private List<ProductListVM> GetMapDatas(List<Product> products)
        {
            List<ProductListVM> productList = new List<ProductListVM>();
            foreach (var product in products)
            {
                ProductListVM newProduct = new ProductListVM
                {
                    Id = product.Id,
                    Image=product.Images
                        .Where(m=>m.IsDelete==false)
                        .FirstOrDefault()?.Name,
                    Name=product.Name,
                    CategoryName=product.Category.Name,
                    Count=product.Count,
                    Price=product.Price

                };
                productList.Add(newProduct);
            }
            return productList;
        }

        public async Task<IActionResult> Create()
        {

            ViewBag.categories = await GetCategoriesByProduct();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateVM productCreateVM)
        {
            ViewBag.categories = await GetCategoriesByProduct();
            if (!ModelState.IsValid) return View();
            List<ProductImage> imageList = new List<ProductImage>();
            if (productCreateVM.Photos !=null)
            {
                foreach (var photo in productCreateVM.Photos)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                    string path = Path.Combine(_environment.WebRootPath, "img", fileName);
                    using (FileStream stream = new FileStream(path, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }
                    ProductImage productImage = new ProductImage
                    {
                        Name = fileName
                    };
                    imageList.Add(productImage);

                }
            }
            
            Product product = new Product
            {
                Name=productCreateVM.Name,
                Count=productCreateVM.Count,
                Price=productCreateVM.Price,
                CategoryId=productCreateVM.CategoryId,
                Images=imageList
            };
            await _context.ProductImages.AddRangeAsync(imageList);
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            Product product = await _context.Products
                .Include(m => m.Images)
                .Include(m => m.Category)
                .Where(m => m.Id == id && !m.IsDelete)
                .FirstOrDefaultAsync();
            if (product == null) return NotFound();
            foreach (var item in product.Images)
            {
                string path = Path.Combine(_environment.WebRootPath, "img", item.Name);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                item.IsDelete = true;
            }
            
            
            product.IsDelete = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.categories = await GetCategoriesByProduct();
            Product product = _context.Products
                .Include(m=>m.Images)
                .Include(m=>m.Category)
                .Where(m => m.Id == id)
                .FirstOrDefault();
            ProductUpdateVM result = new ProductUpdateVM
            {
                Name=product.Name,
                Count=product.Count,
                CategoryId=product.CategoryId,
                ProductImages=product.Images,
                Price=product.Price
            };

            return View(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit (int id,ProductUpdateVM updateVM)
        {
            
            ViewBag.categories = await GetCategoriesByProduct();
            if (!ModelState.IsValid) return View(updateVM);
            Product product = _context.Products
                .Include(m => m.Images)
                .Include(m => m.Category)
                .Where(m => m.Id == id)
                .FirstOrDefault();
            if (product == null) return NotFound();

            List<ProductImage> imageList = new List<ProductImage>();

            if (updateVM.Photos == null)
            {
                foreach (var photo in updateVM.ProductImages)
                {
                    string path = Path.Combine(_environment.WebRootPath, "img", photo.Name);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                    photo.IsDelete = true;
                }
                foreach (var photo in updateVM.Photos)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                    string path = Path.Combine(_environment.WebRootPath, "img", fileName);
                    using (FileStream stream = new FileStream(path, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }
                    ProductImage productImage = new ProductImage
                    {
                        Name = fileName
                    };
                    imageList.Add(productImage);
                }

                product.Images = imageList;
            }
            
            product.Name = updateVM.Name;
            product.Price = updateVM.Price;
            product.Count = updateVM.Count;
            product.CategoryId = updateVM.CategoryId;
            
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }





        private async Task<SelectList> GetCategoriesByProduct()
        {
            var categories = await _context.Categories.Where(m => !m.IsDelete).ToListAsync();
            return new SelectList(categories, "Id", "Name");
        }

    }
}
