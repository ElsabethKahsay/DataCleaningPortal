using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Controllers;
using ADDPerformance.Data;
using ADDPerformance.Services;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using ADDPerformance.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using System.Threading;

namespace ADDPerformance.Tests
{
    public class HomeControllerTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IAddCkService> _mockAddCkService;
        private readonly DBContext _dbContext;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            var userStoreMock = new Mock<IUserStore<IdentityUser>>();
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockAddCkService = new Mock<IAddCkService>();

            var options = new DbContextOptionsBuilder<DBContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString()) // Unique DB for each test class/run
                .Options;
            _dbContext = new DBContext(options);

            _controller = new HomeController(
                _mockUserManager.Object,
                _dbContext,
                _mockEnvironment.Object,
                _mockAddCkService.Object
            );
            
            // Setup User context
             var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "test-user")
            }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task Upload_ReturnsBadRequest_WhenFileIsNull()
        {
            var model = new FileUploadDto { File = null };
            var result = await _controller.Upload(model);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_ReturnsBadRequest_WhenFileTypeChoiceIsEmpty()
        {
           var fileMock = new Mock<IFormFile>();
           fileMock.Setup(f => f.Length).Returns(100);
           
            var model = new FileUploadDto 
            { 
                File = fileMock.Object,
                FileTypeChoice = ""
            };
            var result = await _controller.Upload(model);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_ReturnsBadRequest_WhenNotCsv()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("test.txt");

            var model = new FileUploadDto 
            { 
                File = fileMock.Object,
                FileTypeChoice = "1"
            };
            var result = await _controller.Upload(model);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_ReturnsBadRequest_WhenFileTooLarge()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(4 * 1024 * 1024); // 4MB
            fileMock.Setup(f => f.FileName).Returns("test.csv");

            var model = new FileUploadDto 
            { 
                File = fileMock.Object,
                FileTypeChoice = "1"
            };
            var result = await _controller.Upload(model);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_ReturnsOk_WhenValid_Type1()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("test.csv");

            var model = new FileUploadDto 
            { 
                File = fileMock.Object,
                FileTypeChoice = "1"
            };
            
            var importResult = new ImportResult { Success = true, Message = "Uploaded" };

             _mockAddCkService.Setup(s => s.ProcessAddCkCsvAsync(It.IsAny<IFormFile>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResult);

            var result = await _controller.Upload(model);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(importResult, okResult.Value);
        }
        
         [Fact]
        public async Task Upload_ReturnsBadRequest_WhenInvalidType()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("test.csv");

            var model = new FileUploadDto 
            { 
                File = fileMock.Object,
                FileTypeChoice = "99"
            };

            var result = await _controller.Upload(model);
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
