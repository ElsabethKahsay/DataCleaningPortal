using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Controllers.Api;
using ADDPerformance.Data;
using ADDPerformance.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace ADDPerformance.Tests
{
    public class OnlineSalesControllerTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly DBContext _context;
        private readonly OnlineSalesController _controller;

        public OnlineSalesControllerTests()
        {
             var userStoreMock = new Mock<IUserStore<IdentityUser>>();
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var options = new DbContextOptionsBuilder<DBContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new DBContext(options);

            _controller = new OnlineSalesController(_mockUserManager.Object, _context);
            
            // Mock user
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
            var result = await _controller.Upload(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_ParsesCsvAndAddsRecords()
        {
            var content = "Date,CY,LY,Target\nJAN-2024,100,80,110";
            var fileName = "test.csv";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.OpenReadStream()).Returns(stream);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(stream.Length);

            var result = await _controller.Upload(fileMock.Object);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            var saved = await _context.OnlineSales.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("JAN", saved.Month);
            Assert.Equal(2024, saved.Year);
            Assert.Equal(100, saved.CYPercent);
        }
    }
}
