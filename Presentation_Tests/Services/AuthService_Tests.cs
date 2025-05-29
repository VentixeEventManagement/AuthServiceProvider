using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Presentation.Services;
using Presentation.Models;
using Presentation.Interfaces;
using Moq.Protected;
using System.Text;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using Grpc.Core;
using System.Net.Http;
using Presentation;
using Microsoft.AspNetCore.Routing;

public class AuthServiceTests
{
    private readonly Mock<AccountGrpcService.AccountGrpcServiceClient> _mockAccountClient;
    private readonly Mock<IAuthServiceBusHandler> _mockServiceBus;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IOptions<ApiSettings>> _mockApiSettings;

    private readonly ApiSettings _apiSettings = new ApiSettings
    {
        verificationCodeKey = "dummy-key"
    };
    private readonly IAuthService _authService;

    public AuthServiceTests()
    {
        _mockAccountClient = new Mock<AccountGrpcService.AccountGrpcServiceClient>();
        _mockServiceBus = new Mock<IAuthServiceBusHandler>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockApiSettings = new Mock<IOptions<ApiSettings>>();
        _mockApiSettings.Setup(a => a.Value).Returns(_apiSettings);

        var httpClient = CreateHttpClient(HttpStatusCode.OK, "");
        _authService = CreateAuthService(httpClient);
    }

    private HttpClient CreateHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });

        return new HttpClient(handler.Object);
    }

    private AuthService CreateAuthService(HttpClient httpClient)
    {
        _mockApiSettings.Setup(x => x.Value).Returns(_apiSettings);

        return new AuthService(
            _mockAccountClient.Object,
            _mockServiceBus.Object,
            httpClient,
            _mockConfiguration.Object,
            _mockApiSettings.Object);
    }

    // Verification ----------------------------------------------

    [Fact]
    public async Task VerificationCodeRequestAsync_ShouldReturnSuccess_WhenPublishSucceeds()
    {
        var email = "bjorn@domain.com";

        _mockServiceBus.Setup(s => s.PublishAsync(email)).Returns(Task.CompletedTask);

        var authService = CreateAuthService(new HttpClient());

        var result = await authService.VerificationCodeRequestAsync(email);

        Assert.True(result.Succeeded);
        Assert.Equal("Verification code sent to email.", result.Message);
        _mockServiceBus.Verify(s => s.PublishAsync(email), Times.Once);
    }

    [Fact]
    public async Task VerificationCodeRequestAsync_ShouldReturnFailure_WhenPublishThrowsException()
    {
        var email = "bjorn@domain.com";
        var exceptionMessage = "Some error";

        _mockServiceBus.Setup(s => s.PublishAsync(email)).ThrowsAsync(new Exception(exceptionMessage));

        var authService = CreateAuthService(new HttpClient());

        var result = await authService.VerificationCodeRequestAsync(email);

        Assert.False(result.Succeeded);
        Assert.Equal(exceptionMessage, result.Message);
        _mockServiceBus.Verify(s => s.PublishAsync(email), Times.Once);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsError_WhenFormDataIsNull()
    {
        var service = CreateAuthService(new HttpClient());

        var result = await service.VerifyCodeAsync(null);

        Assert.False(result.Succeeded);
        Assert.Equal("Email and verification code are required.", result.Message);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsError_WhenEmailOrCodeIsMissing()
    {
        var service = CreateAuthService(new HttpClient());

        var result = await service.VerifyCodeAsync(new VerifyForm { Email = "", Code = "" });

        Assert.False(result.Succeeded);
        Assert.Equal("Email and verification code are required.", result.Message);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsError_WhenResponseIsErrorWithJsonMessage()
    {
        var errorJson = JsonSerializer.Serialize(new Dictionary<string, string> { { "message", "Invalid code" } });
        var client = CreateHttpClient(HttpStatusCode.BadRequest, errorJson);
        var service = CreateAuthService(client);

        var result = await service.VerifyCodeAsync(new VerifyForm { Email = "bjorn@domain.com", Code = "1234" });

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid code", result.Message);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsError_WhenResponseIsErrorWithRawText()
    {
        var client = CreateHttpClient(HttpStatusCode.BadRequest, "Raw error text");
        var service = CreateAuthService(client);

        var result = await service.VerifyCodeAsync(new VerifyForm { Email = "bjorn@domain.com", Code = "1234" });

        Assert.False(result.Succeeded);
        Assert.Equal("Raw error text", result.Message);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsSuccess_WhenResponseIsOk()
    {
        var client = CreateHttpClient(HttpStatusCode.OK, "");
        var service = CreateAuthService(client);

        var result = await service.VerifyCodeAsync(new VerifyForm { Email = "bjorn@domain.com", Code = "1234" });

        Assert.True(result.Succeeded);
        Assert.Equal("The account is verified", result.Message);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsError_WhenHttpRequestExceptionOccurs()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network down"));

        var client = new HttpClient(handler.Object);
        var service = CreateAuthService(client);

        var result = await service.VerifyCodeAsync(new VerifyForm { Email = "bjorn@domain.com", Code = "1234" });

        Assert.False(result.Succeeded);
        Assert.Contains("Network error", result.Message);
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsError_WhenGenericExceptionOccurs()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Something went wrong"));

        var client = new HttpClient(handler.Object);
        var service = CreateAuthService(client);

        var result = await service.VerifyCodeAsync(new VerifyForm { Email = "bjorn@domain.com", Code = "1234" });

        Assert.False(result.Succeeded);
        Assert.Equal("Something went wrong", result.Message);
    }

    // Sign up ----------------------------------------------------------

    [Fact]
    public async Task SignUpAsync_ReturnsSuccessResult_WhenCreateAccountSucceeds()
    {
        var formData = new SignUpForm { Email = "test@test.com", Password = "password" };

        var grpcReply = new CreateAccountReply
        {
            Succeeded = true,
            Message = "Account created",
            UserId = "user123"
        };

        var responseTask = Task.FromResult(grpcReply);

        var asyncUnaryCall = new AsyncUnaryCall<CreateAccountReply>(
            responseTask,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient.Setup(c => c.CreateAccountAsync(
            It.IsAny<CreateAccountRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);


        var result = await _authService.SignUpAsync(formData);

        Assert.True(result.Succeeded);
        Assert.Equal("Account created", result.Message);
        Assert.Equal("user123", result.UserId);
    }

    [Fact]
    public async Task SignUpAsync_ReturnsFailureResult_WhenCreateAccountFails()
    {
        var formData = new SignUpForm { Email = "error@test.com", Password = "password" };

        var failedTask = Task.FromException<CreateAccountReply>(new Exception("GRPC failure"));

        var asyncUnaryCall = new AsyncUnaryCall<CreateAccountReply>(
            failedTask,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient.Setup(c => c.CreateAccountAsync(
            It.IsAny<CreateAccountRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        var result = await _authService.SignUpAsync(formData);

        Assert.False(result.Succeeded);
        Assert.Equal("GRPC failure", result.Message);
        Assert.Null(result.UserId);
    }


    [Fact]
    public async Task SignUpAsync_ReturnsFailureResult_WhenExceptionIsThrown()
    {
        var formData = new SignUpForm { Email = "error@test.com", Password = "password" };

        var failedTask = Task.FromException<CreateAccountReply>(new Exception("GRPC failure"));

        var asyncUnaryCall = new AsyncUnaryCall<CreateAccountReply>(
            failedTask,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient.Setup(c => c.CreateAccountAsync(
            It.IsAny<CreateAccountRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        var result = await _authService.SignUpAsync(formData);

        Assert.False(result.Succeeded);
        Assert.Equal("GRPC failure", result.Message);
        Assert.Null(result.UserId);
    }

    // Sign in ---------------------------------------------------------------------------
    [Fact]
    public async Task SignInAsync_ReturnsSuccessResult_WhenCredentialsAndAccountAreValid()
    {
        var formData = new SignInForm { Email = "user@test.com", Password = "password" };

        var validateReply = new ValidateCredentialsReply
        {
            Succeeded = true,
            Message = "Valid credentials",
            UserId = "user123"
        };

        var getAccountReply = new GetAccountReply
        {
            Succeeded = true,
            Account = new Account { RoleName = "Admin" }
        };

        var validateCall = new AsyncUnaryCall<ValidateCredentialsReply>(
            Task.FromResult(validateReply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        var getAccountCall = new AsyncUnaryCall<GetAccountReply>(
            Task.FromResult(getAccountReply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient.Setup(c => c.ValidateCredentialsAsync(
            It.IsAny<ValidateCredentialsRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(validateCall);

        _mockAccountClient.Setup(c => c.GetAccountAsync(
            It.Is<GetAccountRequest>(r => r.UserId == "user123"),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(getAccountCall);

        var result = await _authService.SignInAsync(formData);

        Assert.True(result.Succeeded);
        Assert.Equal("Valid credentials", result.Message);
        Assert.Equal("user123", result.UserId);
        Assert.Equal("Admin", result.RoleName);
    }

    [Fact]
    public async Task SignInAsync_ReturnsFailureResult_WhenCredentialsAreInvalid()
    {
        // Arrange
        var formData = new SignInForm
        {
            Email = "wrong@test.com",
            Password = "wrongpassword"
        };

        var grpcReply = new ValidateCredentialsReply
        {
            Succeeded = false,
            Message = "Invalid credentials"
        };

        var responseTask = Task.FromResult(grpcReply);

        var asyncUnaryCall = new AsyncUnaryCall<ValidateCredentialsReply>(
            responseTask,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient.Setup(x => x.ValidateCredentialsAsync(
            It.IsAny<ValidateCredentialsRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        // Act
        var result = await _authService.SignInAsync(formData);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Invalid credentials", result.Message);
        Assert.Null(result.UserId);
        Assert.Null(result.RoleName);
    }

    // Get all accounts
    [Fact]
    public async Task GetAllAccountsAsync_ReturnsMappedAccounts_WhenGrpcCallSucceeds()
    {
        var grpcAccounts = new List<Account>
    {
        new Account { UserId = "1", Email = "a@domain.com", PhoneNumber = "123", RoleName = "Admin" },
        new Account { UserId = "2", Email = "b@domain.com", PhoneNumber = "456", RoleName = "User" }
    };

        var grpcReply = new GetAccountsReply { Accounts = { grpcAccounts } };

        var asyncUnaryCall = new AsyncUnaryCall<GetAccountsReply>(
            Task.FromResult(grpcReply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient.Setup(c => c.GetAccountsAsync(
            It.IsAny<GetAccountsRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        var accounts = await _authService.GetAllAccountsAsync();

        Assert.Collection(accounts,
            a =>
            {
                Assert.Equal("1", a.UserId);
                Assert.Equal("a@domain.com", a.Email);
                Assert.Equal("123", a.PhoneNumber);
                Assert.Equal("Admin", a.RoleName);
            },
            a =>
            {
                Assert.Equal("2", a.UserId);
                Assert.Equal("b@domain.com", a.Email);
                Assert.Equal("456", a.PhoneNumber);
                Assert.Equal("User", a.RoleName);
            });
    }

    [Fact]
    public async Task GetAllAccountsAsync_ReturnsEmptyList_WhenNoAccountsInGrpcReply()
    {
        var grpcReply = new GetAccountsReply(); // tom lista

        var asyncUnaryCall = new AsyncUnaryCall<GetAccountsReply>(
            Task.FromResult(grpcReply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient.Setup(c => c.GetAccountsAsync(
            It.IsAny<GetAccountsRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        var accounts = await _authService.GetAllAccountsAsync();

        Assert.NotNull(accounts);
        Assert.Empty(accounts);
    }

    [Fact]
    public async Task GetAllAccountsAsync_ThrowsException_WhenGrpcCallFails()
    {
        var asyncUnaryCall = new AsyncUnaryCall<GetAccountsReply>(
            Task.FromException<GetAccountsReply>(new Exception("GRPC error")),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient.Setup(c => c.GetAccountsAsync(
            It.IsAny<GetAccountsRequest>(),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        var exception = await Assert.ThrowsAsync<Exception>(() => _authService.GetAllAccountsAsync());

        Assert.Equal("GRPC error", exception.Message);
    }

    // Get account
    [Fact]
    public async Task GetAccountInfoAsync_ReturnsAccount_WhenReplySucceeded()
    {
        // Arrange
        var userId = "user123";

        var reply = new GetAccountReply
        {
            Succeeded = true,
            Message = "Ok",
            Account = new Account
            {
                UserId = userId,
                Email = "test@example.com",
                PhoneNumber = "123456789",
                RoleName = "User"
            }
        };

        var asyncUnaryCall = new AsyncUnaryCall<GetAccountReply>(
            Task.FromResult(reply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient
            .Setup(c => c.GetAccountAsync(It.Is<GetAccountRequest>(r => r.UserId == userId), null, null, default))
            .Returns(asyncUnaryCall);

        // Act
        var result = await _authService.GetAccountInfoAsync(userId);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Account info retrieved", result.Message);
        Assert.NotNull(result.Account);
        Assert.Equal(userId, result.Account.UserId);
        Assert.Equal("test@example.com", result.Account.Email);
        Assert.Equal("123456789", result.Account.PhoneNumber);
        Assert.Equal("User", result.Account.RoleName);
    }

    [Fact]
    public async Task GetAccountInfoAsync_ReturnsFailure_WhenReplyNotSucceeded()
    {
        // Arrange
        var reply = new GetAccountReply
        {
            Succeeded = false,
            Message = "User not found"
        };

        var asyncUnaryCall = new AsyncUnaryCall<GetAccountReply>(
            Task.FromResult(reply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient
            .Setup(c => c.GetAccountAsync(It.IsAny<GetAccountRequest>(), null, null, default))
            .Returns(asyncUnaryCall);

        // Act
        var result = await _authService.GetAccountInfoAsync("user123");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("User not found", result.Message);
        Assert.Null(result.Account);
    }


    // Update role
    [Fact]
    public async Task UpdateRoleAsync_ReturnsSuccess_WhenReplySucceeded()
    {
        // Arrange
        var userId = "user123";
        var newRole = "Admin";

        var reply = new ChangeUserRoleReply
        {
            Message = "Role updated successfully"
        };

        var asyncUnaryCall = new AsyncUnaryCall<ChangeUserRoleReply>(
            Task.FromResult(reply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient
            .Setup(c => c.ChangeUserRoleAsync(It.Is<ChangeUserRoleRequest>(r => r.UserId == userId && r.NewRole == newRole), null, null, default))
            .Returns(asyncUnaryCall);

        // Act
        var result = await _authService.UpdateRoleAsync(userId, newRole);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("Role updated successfully", result.Message);
    }

    [Fact]
    public async Task UpdateRoleAsync_ReturnsFailure_WhenReplyMessageIndicatesFailure()
    {
        // Arrange
        var userId = "user123";
        var newRole = "Admin";

        var reply = new ChangeUserRoleReply
        {
            Message = "User not found"
        };

        var asyncUnaryCall = new AsyncUnaryCall<ChangeUserRoleReply>(
            Task.FromResult(reply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );

        _mockAccountClient
            .Setup(c => c.ChangeUserRoleAsync(It.IsAny<ChangeUserRoleRequest>(), null, null, default))
            .Returns(asyncUnaryCall);

        // Act
        var result = await _authService.UpdateRoleAsync(userId, newRole);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("User not found", result.Message);
    }



}
