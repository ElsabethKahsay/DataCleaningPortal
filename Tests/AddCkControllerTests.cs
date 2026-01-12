using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Controllers.Api;
using ADDPerformance.Data;
using ADDPerformance.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using ADDPerformance.Models;
using System.Collections.Generic;
using ImportResult = ADDPerformance.Models.ImportResult;

namespace ADDPerformance.Tests
{
    public class AddCkControllerTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly Mock<IAddCkService> _mockAddCkService;
        private readonly DBContext _context;
        private readonly AddCkController _controller;

        public AddCkControllerTests()
        {
             var userStoreMock = new Mock<IUserStore<IdentityUser>>();
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockAddCkService = new Mock<IAddCkService>();

            var options = new DbContextOptionsBuilder<DBContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new DBContext(options);

            _controller = new AddCkController(_context, _mockUserManager.Object, _mockAddCkService.Object);
            
             _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
         public async Task GetById_ReturnsNotFound_WhenRecordDoesNotExist()
        {
            var result = await _controller.GetById(12345); // Non-existent ID
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenRecordExists()
        {
            var entity = new ADD_CK { Id = 1, Date = System.DateTime.Now, Status = Status.Active };
            _context.ADD_CK.Add(entity);
            await _context.SaveChangesAsync();

            var result = await _controller.GetById(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedItem = Assert.IsType<ADD_CK>(okResult.Value);
            Assert.Equal(entity.Id, returnedItem.Id);
        }
        
        [Fact]
        public async Task Upload_ReturnsOk_WhenServiceReturnsResult()
        {
             var fileMock = new Mock<IFormFile>();
             var importResult = new ImportResult { Message = "true" };
             _mockAddCkService.Setup(s => s.ProcessAddCkCsvAsync(It.IsAny<IFormFile>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(importResult);

            var result = await _controller.UploadCsv(fileMock.Object);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
