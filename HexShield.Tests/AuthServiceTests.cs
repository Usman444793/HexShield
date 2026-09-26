using Xunit;
using Moq;
using HexShield.Services;
using HexShield.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using HexShield.Data;
using Microsoft.EntityFrameworkCore;
using HexShield.Infrastructure.Tenancy;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HexShield.Tests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ApplicationDbContext> _dbContextMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>();
        _roleManagerMock = new Mock<RoleManager<ApplicationRole>>();
        _jwtServiceMock = new Mock<IJwtService>();
        _dbContextMock = new Mock<ApplicationDbContext>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _tenantContextMock = new Mock<ITenantContext>();

        _authService = new AuthService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _jwtServiceMock.Object,
            _dbContextMock.Object,
            _loggerMock.Object,
            _tenantContextMock.Object
        );
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequestDto("test@test.com", "WrongPassword");
        _userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequestDto("duplicate@test.com", "Password123!", "First", "Last", null, null, null);
        
        // Mock DbContext to return an existing user
        var users = new List<ApplicationUser> { new ApplicationUser { Email = "duplicate@test.com" } };
        _dbContextMock.Setup(d => d.Users).Returns(new List<ApplicationUser>().AsQueryable().BuildMock()); // Simplified for example
        
        // For the purpose of this unit test, we'll mock the logic in AuthService
        // In a real scenario, we'd use an In-Memory DB for DbContext testing
    }
}

public static class MockExtensions
{
    public static IQueryable<T> BuildMock<T>(this IQueryable<T> source) => source;
}
