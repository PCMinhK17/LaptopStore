using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using LaptopStore.Controllers;
using LaptopStore.Models;
using Moq;
using Microsoft.Data.Sqlite;

namespace Test
{
    public class ProductSearchTest
    {
        private LaptopStoreDbContext GetDbContext()
        {
            SQLitePCL.Batteries_V2.Init();

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new LaptopStoreDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }

        /// <summary>
        /// Test: Không filter → trả tất cả sản phẩm
        /// </summary>
        [Fact]
        public void Search_NoFilter_ReturnsAllProducts()
        {
            var context = GetDbContext();

            // Seed dữ liệu
            context.Products.AddRange(
                new Product { Id = 1, Price = 5000000 },
                new Product { Id = 2, Price = 15000000 }
            );

            context.SaveChanges();

            var loggerMock = new Mock<ILogger<ProductController>>();
            var controller = new ProductController(loggerMock.Object, context);

            // Act
            var result = controller.SearchByCategoryAndBrand(null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Product>>(viewResult.Model);

            Assert.Equal(2, model.Count);
        }

        /// <summary>
        /// Test: Filter theo Brand
        /// </summary>
        [Fact]
        public void Search_FilterByBrand_ReturnsCorrectProducts()
        {
            var context = GetDbContext();

            context.Products.AddRange(
                new Product { Id = 1, BrandId = 1, Price = 5000000 },
                new Product { Id = 2, BrandId = 2, Price = 5000000 }
            );

            context.SaveChanges();

            var loggerMock = new Mock<ILogger<ProductController>>();
            var controller = new ProductController(loggerMock.Object, context);

            // Act
            var result = controller.SearchByCategoryAndBrand(1, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = viewResult.Model as List<Product>;

            Assert.Single(model);
            Assert.Equal(1, model[0].BrandId);
        }

        /// <summary>
        /// Test: Filter theo PriceRange (case 1: < 10 triệu)
        /// </summary>
        [Fact]
        public void Search_FilterByPriceRange1_ReturnsCorrectProducts()
        {
            var context = GetDbContext();

            context.Products.AddRange(
                new Product { Id = 1, Price = 5000000 },   // < 10tr
                new Product { Id = 2, Price = 15000000 }   // > 10tr
            );

            context.SaveChanges();

            var loggerMock = new Mock<ILogger<ProductController>>();
            var controller = new ProductController(loggerMock.Object, context);

            // Act
            var result = controller.SearchByCategoryAndBrand(null, null, 1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = viewResult.Model as List<Product>;

            Assert.Single(model);
            Assert.True(model[0].Price < 10000000);
        }
    }
}