using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ADDPerformance.Models;

namespace ADDPerformance.Services
{
    public interface IAddCkService
    {
        Task<ImportResult> ProcessAddCkCsvAsync(IFormFile file, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    }
}
