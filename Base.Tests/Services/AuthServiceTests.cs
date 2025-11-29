/*using Base.DAL.Models;
using Base.Repo.Interfaces;
using Base.Services.Implementations;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq; // مطلوب لـ Select(e => e.Description)
using System.Threading.Tasks;
using Xunit;

namespace Base.Tests.Services
{
    public class AuthServiceFullTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IOtpService> _otpServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly AuthService _authService;
        private readonly Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> _transactionMock;
        public AuthServiceFullTests()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            // Mock UserManager
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);

            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

            _emailSenderMock = new Mock<IEmailSender>();
            _otpServiceMock = new Mock<IOtpService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _configMock = new Mock<IConfiguration>();

            // JWT setup required for token generation in LoginUserAsync/VerifyLoginAsync
            _configMock.Setup(x => x["Jwt:Key"]).Returns("12345678901234567890123456789012");
            _configMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
            _configMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");
            _configMock.Setup(x => x["DefaultRoles:User"]).Returns("User"); // Required for external login/register


            // Mocking the transaction setup
            _transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_transactionMock.Object);

            _authService = new AuthService(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _emailSenderMock.Object,
                _otpServiceMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _configMock.Object
            );
        }

        #region LoginUserAsync Tests
        [Fact]
        public async Task LoginUserAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
        {
            var user = new ApplicationUser { Id = "1", Email = "test@example.com", UserName = "TestUser" };
            var loginDto = new LoginDTO { Email = user.Email, Password = "Password123!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            var result = await _authService.LoginUserAsync(loginDto);

            Assert.True(result.Success);
            Assert.Equal("Login successful.", result.Message);
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
        }

        [Fact]
        public async Task LoginUserAsync_ShouldRequireOtp_WhenTwoFactorIsEnabled()
        {
            var user = new ApplicationUser { Id = "2", Email = "otp@example.com", PhoneNumber = "1234567890" };
            var loginDto = new LoginDTO { Email = user.Email, Password = "Password123!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
            // OTP setup
            _otpServiceMock.Setup(x => x.GenerateAndStoreOtpAsync(user.Id, user.Email)).ReturnsAsync("123456");
            _emailSenderMock.Setup(x => x.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await _authService.LoginUserAsync(loginDto);

            Assert.True(result.Success);
            Assert.True(result.RequiresOtpVerification);
            Assert.Equal("Credentials accepted. A One-Time Password (OTP) has been sent to your email.", result.Message);
            _otpServiceMock.Verify(x => x.GenerateAndStoreOtpAsync(user.Id, user.Email), Times.Once);
        }

        [Fact]
        public async Task LoginUserAsync_ShouldFail_WhenPasswordInvalid()
        {
            var user = new ApplicationUser { Id = "3", Email = "fail@example.com" };
            var loginDto = new LoginDTO { Email = user.Email, Password = "WrongPassword" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(false);

            var result = await _authService.LoginUserAsync(loginDto);

            Assert.False(result.Success);
            Assert.Equal("Invalid credentials.", result.Message);
        }

        // ⭐ اختبار جديد: المستخدم غير موجود
        [Fact]
        public async Task LoginUserAsync_ShouldFail_WhenUserNotFound()
        {
            var loginDto = new LoginDTO { Email = "nonexistent@example.com", Password = "Password123!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync((ApplicationUser)null);

            var result = await _authService.LoginUserAsync(loginDto);

            Assert.False(result.Success);
            Assert.Equal("Invalid credentials.", result.Message);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        // ⭐ اختبار جديد: الإيميل غير مؤكد
        [Fact]
        public async Task LoginUserAsync_ShouldFail_WhenEmailNotConfirmed()
        {
            var user = new ApplicationUser { Id = "4", Email = "unconfirmed@example.com" };
            var loginDto = new LoginDTO { Email = user.Email, Password = "Password123!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false); // <--- الفشل هنا
            _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);

            var result = await _authService.LoginUserAsync(loginDto);

            Assert.False(result.Success);
            Assert.StartsWith("Your account is not confirmed.", result.Message);
        }

        // ⭐ اختبار جديد: المستخدم مُغلق حسابه
        [Fact]
        public async Task LoginUserAsync_ShouldFail_WhenUserLockedOut()
        {
            var user = new ApplicationUser { Id = "5", Email = "locked@example.com" };
            var loginDto = new LoginDTO { Email = user.Email, Password = "Password123!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(true); // <--- الفشل هنا

            var result = await _authService.LoginUserAsync(loginDto);

            Assert.False(result.Success);
            Assert.StartsWith("Your account is locked.", result.Message);
        }

        #endregion

        #region VerifyLoginAsync Tests
        [Fact]
        public async Task VerifyLoginAsync_ShouldReturnToken_WhenOtpValid()
        {
            var user = new ApplicationUser { Id = "10", Email = "otpuser@example.com", UserName = "OtpUser" };
            var otpDto = new VerifyOtpDTO { Email = user.Email, Otp = "123456" };

            // OTP Setup: Validation succeeds and returns UserId
            _otpServiceMock.Setup(x => x.ValidateOtpAsync(user.Email, otpDto.Otp)).ReturnsAsync((true, user.Id));

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            // OTP Cleanup
            _otpServiceMock.Setup(x => x.RemoveOtpAsync(user.Email)).Returns(Task.CompletedTask);

            var result = await _authService.VerifyLoginAsync(otpDto);

            Assert.True(result.Success);
            Assert.NotNull(result.Token);
            Assert.False(result.RequiresOtpVerification);
            _otpServiceMock.Verify(x => x.RemoveOtpAsync(user.Email), Times.Once);
        }

        [Fact]
        public async Task VerifyLoginAsync_ShouldFail_WhenOtpInvalid()
        {
            var otpDto = new VerifyOtpDTO { Email = "fail@example.com", Otp = "000000" };

            // OTP Setup: Validation fails and returns null Id
            _otpServiceMock.Setup(x => x.ValidateOtpAsync(otpDto.Email, otpDto.Otp)).ReturnsAsync((false, null));

            var result = await _authService.VerifyLoginAsync(otpDto);

            Assert.False(result.Success);
            Assert.Equal("Invalid OTP. Please try again later.", result.Message);
            Assert.Null(result);
            _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        // ⭐ اختبار جديد: فشل العثور على المستخدم بعد التحقق من الـ OTP
        public async Task VerifyLoginAsync_ShouldThrowInvalidOperation_WhenUserNotFoundAfterOtpValidation()
        {
            var userEmail = "missinguser@example.com";
            var userId = "11";
            var otpDto = new VerifyOtpDTO { Email = userEmail, Otp = "123456" };

            _otpServiceMock.Setup(x => x.ValidateOtpAsync(userEmail, otpDto.Otp)).ReturnsAsync((true, userId));
            // المستخدم موجود في الـ OTP لكن غير موجود في قاعدة البيانات
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.VerifyLoginAsync(otpDto));

            _otpServiceMock.Verify(x => x.RemoveOtpAsync(userEmail), Times.Once); // يجب أن يتم تنظيف الـ OTP حتى لو فشل العثور على المستخدم
        }
        #endregion

        #region RegisterAsync Tests
        // -------------------------------------------------------------
        // 1. سيناريو النجاح الكامل (Happy Path)
        // -------------------------------------------------------------

        [Fact]
        public async Task RegisterAsync_ShouldSucceed_AndCommitTransaction()
        {
            // Arrange
            var registerDto = new RegisterDTO { Email = "success@test.com", Password = "Password123!", FullName = "Test User" };
            var newUser = new ApplicationUser { Id = "123", Email = registerDto.Email };

            // Mock Mappers and Helpers to simulate success
            // 💡 ملاحظة: بما أن الدوال المساعدة (مثل MapAndCreateUser) هي دوال خاصة أو منفصلة، 
            // يجب أن نستخدم Mock لـ AuthService نفسها أو نعتمد على سلوكها الافتراضي إذا كانت داخل AuthService.
            // لغرض هذا الاختبار، سنعتمد على أن الدالة الرئيسية هي التي يتم اختبارها وسنستخدم Mocks للـ Dependencies.

            // 1. FindByEmailAsync returns null (User not exists)
            _userManagerMock.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser)null);

            // 2. Identity Creation (simulate MapAndCreateUser success)
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .Callback<ApplicationUser, string>((user, pass) => user.Id = "123")
                .ReturnsAsync(IdentityResult.Success);

            // 3. Role Assignment (simulate AssignUserRoleAsync success)
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // 4. Profile Creation (simulate CreateUserProfileAsync success)
            var profileRepositoryMock = new Mock<IGenericRepository<UserProfile>>();
            _unitOfWorkMock.Setup(u => u.Repository<UserProfile>()).Returns(profileRepositoryMock.Object);
            profileRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserProfile>())).ReturnsAsync(new UserProfile());

            // 5. Commit Transaction (CompleteAsync returns > 0)
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // 6. Post-Registration Action (simulate SendRegistrationOtpIfPossible success)
            // بما أن OTP/Email non-critical، لا نحتاج لـ Mock قوي، لكن نتحقق من استدعائه

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert

            // Verify Identity and Profile steps
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password), Times.Once);
            profileRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserProfile>()), Times.Once);

            // Verify Transaction steps
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
            _transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
            _transactionMock.Verify(t => t.RollbackAsync(default), Times.Never); // No rollback on success

            // Verify Post-Registration step (SendRegistrationOtpIfPossible)
            // (Verification depends on how SendRegistrationOtpIfPossible is implemented, 
            // but for now, we just ensure the service completed without critical failure)
        }

        // -------------------------------------------------------------
        // 2. سيناريو فشل Identity (Rollback)
        // -------------------------------------------------------------

        [Fact]
        public async Task RegisterAsync_ShouldRollback_WhenIdentityCreationFails()
        {
            // Arrange
            var registerDto = new RegisterDTO { Email = "fail@test.com", Password = "Password123!" , FullName = "Test User"};
            var errors = new IdentityError[] { new IdentityError { Description = "Password too short." } };

            _userManagerMock.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser)null);

            // Identity Creation fails
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(errors));

            // Act & Assert
            // Expecting BadRequestException thrown directly from the try block
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _authService.RegisterAsync(registerDto));

            // Verify Rollback
            _transactionMock.Verify(t => t.RollbackAsync(default), Times.Once); // Rollback must be called
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);        // Commit must not be called

            // Verify the error message
            Assert.Contains(errors.First().Description, ex.Message);
        }

        // -------------------------------------------------------------
        // 3. سيناريو فشل قاعدة البيانات (Rollback)
        // -------------------------------------------------------------

        [Fact]
        public async Task RegisterAsync_ShouldRollback_WhenDbCompleteFails()
        {
            // Arrange
            var registerDto = new RegisterDTO { Email = "dbfail@test.com", Password = "Password123!" , FullName = "Test User"};
            var newUser = new ApplicationUser { Id = "123", Email = registerDto.Email };

            _userManagerMock.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .Callback<ApplicationUser, string>((user, pass) => user.Id = "123")
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            var profileRepositoryMock = new Mock<IGenericRepository<UserProfile>>();
            _unitOfWorkMock.Setup(u => u.Repository<UserProfile>()).Returns(profileRepositoryMock.Object);
            profileRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserProfile>())).ReturnsAsync(new UserProfile());


            // Simulate Database Failure by throwing an exception on CompleteAsync
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ThrowsAsync(new InvalidOperationException("DB connection error."));

            // Act & Assert
            // The catch-all block should wrap the exception in InternalServerException after rollback
            await Assert.ThrowsAsync<InternalServerException>(() => _authService.RegisterAsync(registerDto));

            // Verify Rollback
            _transactionMock.Verify(t => t.RollbackAsync(default), Times.Once);
            _transactionMock.Verify(t => t.CommitAsync(default), Times.Never);
        }

        // -------------------------------------------------------------
        // 4. سيناريو فشل التحقق الأولي (Pre-transaction failure)
        // -------------------------------------------------------------

        [Fact]
        public async Task RegisterAsync_ShouldThrowBadRequest_WhenUserAlreadyExists()
        {
            // Arrange
            var registerDto = new RegisterDTO { Email = "existing@test.com", Password = "Password123!" , FullName = "Test User"};
            var existingUser = new ApplicationUser { Id = "456", Email = registerDto.Email };

            // User already exists
            _userManagerMock.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync(existingUser);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _authService.RegisterAsync(registerDto));

            // Verify
            Assert.Equal("This email is already registered.", ex.Message);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never); // No transaction should start
            _transactionMock.Verify(t => t.RollbackAsync(default), Times.Never);
        }
        /*[Fact]
        public async Task RegisterAsync_ShouldSucceed_WhenValidData()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                FullName = "John Doe",
                UserType = "Customer",
                PhoneNumber = "0501234567"
            };

            var profileRepositoryMock = new Mock<IGenericRepository<UserProfile>>();
            profileRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserProfile>())).ReturnsAsync(new UserProfile());

            _unitOfWorkMock.Setup(u => u.Repository<UserProfile>()).Returns(profileRepositoryMock.Object);

            // Mocks for Identity Operations
            _userManagerMock.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser)null);

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .Callback<ApplicationUser, string>((user, pass) => { user.Id = "123"; })
                .ReturnsAsync(IdentityResult.Success);

            _roleManagerMock.Setup(x => x.RoleExistsAsync("User")).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Mocks for Transaction and Commit
            var transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Mocks for OTP/Email
            _otpServiceMock.Setup(x => x.GenerateAndStoreOtpAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("123456");
            _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert: Verification
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password), Times.Once);
            profileRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserProfile>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
            transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
            _emailSenderMock.Verify(x => x.SendEmailAsync(registerDto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldRollback_WhenCreateUserFails()
        {
            // Arrange
            var registerDto = new RegisterDTO { Email = "newuser@example.com", Password = "Password123!", FullName = "John Doe", UserType = "Customer", PhoneNumber = "0501234567" };
            var errors = new IdentityError[] { new IdentityError { Description = "Password too short." } };

            var transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            _userManagerMock.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(errors));

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _authService.RegisterAsync(registerDto));

            // Verify Rollback
            transactionMock.Verify(t => t.RollbackAsync(default), Times.AtMost(2));
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        }

        // ⭐ اختبار جديد: المستخدم موجود مسبقاً
        [Fact]
        public async Task RegisterAsync_ShouldThrowBadRequest_WhenUserAlreadyExists()
        {
            var registerDto = new RegisterDTO { Email = "existing@example.com", Password = "Password123!", FullName = "John Doe",UserType = "Test" };
            var existingUser = new ApplicationUser { Email = registerDto.Email };

            _userManagerMock.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync(existingUser);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _authService.RegisterAsync(registerDto));

            Assert.Contains("This email is already registered.", ex.Message);
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }*/
        #endregion

        #region SendOtpAsync Tests
        [Fact]
        public async Task SendOtpAsync_ShouldCallEmailSender_WhenUserExists()
        {
            var email = "otpuser@example.com";
            var user = new ApplicationUser { Id = "20", Email = email, PhoneNumber = "1234567890" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _otpServiceMock.Setup(x => x.GenerateAndStoreOtpAsync(user.Id, user.Email)).ReturnsAsync("654321");
            _emailSenderMock.Setup(x => x.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            await _authService.SendOtpAsync(email);

            // Assert: Verify OTP was generated and email was sent
            _otpServiceMock.Verify(x => x.GenerateAndStoreOtpAsync(user.Id, user.Email), Times.Once);
            _emailSenderMock.Verify(x => x.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendOtpAsync_ShouldThrowBadRequest_WhenUserNotFound()
        {
            var email = "nonexistent@example.com";

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _authService.SendOtpAsync(email));

            // Assert: Verify no OTP was attempted
            _otpServiceMock.Verify(x => x.GenerateAndStoreOtpAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        #endregion

        #region VerifyEmailAsync Tests (New Section)

        [Fact]
        public async Task VerifyEmailAsync_ShouldSucceed_WhenOtpValid()
        {
            var userEmail = "confirm@example.com";
            var userId = "50";
            var dto = new VerifyOtpDTO { Email = userEmail, Otp = "123456" };
            var user = new ApplicationUser { Id = userId, Email = userEmail };

            // OTP Validation Setup
            _otpServiceMock.Setup(x => x.ValidateOtpAsync(dto.Email, dto.Otp)).ReturnsAsync((true, userId));
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

            // Email Confirmation Setup
            _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false); // تأكد من أنه غير مؤكد قبل التأكيد
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success); // نستخدم الـ Token placeholder هنا لأن الدالة تتوقع توكن

            // OTP Cleanup
            _otpServiceMock.Setup(x => x.RemoveOtpAsync(dto.Email)).Returns(Task.CompletedTask);

            // Act
            var result = await _authService.VerifyEmailAsync(dto);

            // Assert
            Assert.True(result); // نتحقق من الإرجاع false بدلاً من فحص الـ Inner Exception
            _otpServiceMock.Verify(x => x.RemoveOtpAsync(dto.Email), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailAsync_ShouldFail_WhenOtpInvalid()
        {
            var dto = new VerifyOtpDTO { Email = "failconfirm@example.com", Otp = "000000" };

            // OTP Validation Setup: Fails
            _otpServiceMock.Setup(x => x.ValidateOtpAsync(dto.Email, dto.Otp)).ReturnsAsync((false, null));

            // Act
            var result = await _authService.VerifyEmailAsync(dto);

            // Assert
            Assert.False(result);
            _userManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyEmailAsync_ShouldFail_WhenConfirmationFails()
        {
            var userEmail = "confirmfail@example.com";
            var userId = "51";
            var dto = new VerifyOtpDTO { Email = userEmail, Otp = "123456" };
            var user = new ApplicationUser { Id = userId, Email = userEmail };

            // OTP Validation Setup
            _otpServiceMock.Setup(x => x.ValidateOtpAsync(dto.Email, dto.Otp)).ReturnsAsync((true, userId));
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

            // Email Confirmation Setup: Fails
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Confirmation failed." }));

            // Act
            var result = await _authService.VerifyEmailAsync(dto);

            // Assert
            Assert.False(result);
            _otpServiceMock.Verify(x => x.RemoveOtpAsync(dto.Email), Times.Once); // يجب أن يتم تنظيف الـ OTP
        }
        #endregion

        #region ResetPassword Tests
        // ... (ResetPassword tests remain the same as they were adequate) ...
        [Fact]
        public async Task ResetPassword_ShouldSucceed_WhenOtpValid()
        {
            var user = new ApplicationUser { Id = "30", Email = "reset@example.com" };
            var dto = new ResetPasswordDTO { Email = user.Email, Token = "123456", NewPassword = "NewPass123!" };

            // OTP Validation Setup
            _otpServiceMock.Setup(x => x.ValidateOtpAsync(dto.Email, dto.Otp)).ReturnsAsync((true, user.Id));

            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "token", dto.NewPassword)).ReturnsAsync(IdentityResult.Success);

            // OTP Cleanup
            _otpServiceMock.Setup(x => x.RemoveOtpAsync(dto.Email)).Returns(Task.CompletedTask);

            var result = await _authService.ResetPassword(dto);

            Assert.True(result);
            _userManagerMock.Verify(x => x.ResetPasswordAsync(user, "token", dto.NewPassword), Times.Once);
            _otpServiceMock.Verify(x => x.RemoveOtpAsync(dto.Email), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_ShouldFail_WhenResetFails()
        {
            var user = new ApplicationUser { Id = "31", Email = "reset2@example.com" };
            var dto = new ResetPasswordDTO { Email = user.Email, Token = "123456", NewPassword = "NewPass123!" };

            _otpServiceMock.Setup(x => x.ValidateOtpAsync(dto.Email, dto.Otp)).ReturnsAsync((true, user.Id));
            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");

            // Identity Reset fails
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "token", dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Reset failed." }));

            var result = await _authService.ResetPassword(dto);

            Assert.False(result);
        }
        #endregion

        #region ChangePasswordAsync Tests
        [Fact]
        public async Task ChangePasswordAsync_ShouldSucceed_WhenValid()
        {
            var userId = "40";
            var user = new ApplicationUser { Id = userId };
            var dto = new ChangePasswordDTO { CurrentPassword = "OldPass123!", NewPassword = "NewPass123!" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword)).ReturnsAsync(IdentityResult.Success);

            // Act & Assert: يجب ألا يرمي استثناء
            await _authService.ChangePasswordAsync(userId, dto);

            _userManagerMock.Verify(x => x.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowException_WhenIdentityFails()
        {
            var userId = "41";
            var user = new ApplicationUser { Id = userId };
            var dto = new ChangePasswordDTO { CurrentPassword = "OldPass123!", NewPassword = "NewPass123!" };
            var errors = new IdentityError[] { new IdentityError { Description = "Current password mismatch." } };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(errors));

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _authService.ChangePasswordAsync(userId, dto));

            _userManagerMock.Verify(x => x.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword), Times.Once);
        }

        // ⭐ اختبار جديد: عدم العثور على المستخدم
        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowNotFound_WhenUserNotFound()
        {
            var userId = "42";
            var dto = new ChangePasswordDTO { CurrentPassword = "OldPass123!", NewPassword = "NewPass123!" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _authService.ChangePasswordAsync(userId, dto));

            _userManagerMock.Verify(x => x.ChangePasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        #endregion

        #region HandleExternalLoginAsync Tests (New Section)

        [Fact]
        public async Task HandleExternalLoginAsync_ShouldSucceed_WhenUserExists()
        {
            // Arrange
            var email = "existing@google.com";
            var fullName = "External User";
            var existingUser = new ApplicationUser { Id = "60", Email = email, UserName = fullName };

            // Mock Repository for profile check (Optional, depends on your service logic, assuming minimal check here)
            var profileRepositoryMock = new Mock<IGenericRepository<UserProfile>>();
            _unitOfWorkMock.Setup(u => u.Repository<UserProfile>()).Returns(profileRepositoryMock.Object);

            // Identity Mocks
            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(existingUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(existingUser)).ReturnsAsync(new List<string> { "User" });
            _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(existingUser)).ReturnsAsync(false);

            // Act
            var result = await _authService.HandleExternalLoginAsync(email, fullName);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never); // لم يتم إنشاء مستخدم جديد
        }

        [Fact]
        public async Task HandleExternalLoginAsync_ShouldCreateUser_WhenUserNew()
        {
            // Arrange
            var email = "new@facebook.com";
            var fullName = "New External User";

            // Mock Repository and Transaction for creation
            var profileRepositoryMock = new Mock<IGenericRepository<UserProfile>>();
            profileRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserProfile>())).ReturnsAsync(new UserProfile());
            _unitOfWorkMock.Setup(u => u.Repository<UserProfile>()).Returns(profileRepositoryMock.Object);
            var transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Identity Mocks
            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser)null); // المستخدم غير موجود

            // Setup CreateAsync to succeed and assign an ID
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string>((user, pass) => {
                    user.Id = "61";
                    user.Email = email;
                    user.UserName = email; // الافتراض هو تعيين الإيميل كاسم مستخدم
                })
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User")).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _authService.HandleExternalLoginAsync(email, fullName);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once); // تم إنشاء مستخدم جديد
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
            transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
        }

        [Fact]
        public async Task HandleExternalLoginAsync_ShouldThrowBadRequest_WhenEmailIsNullOrEmpty()
        {
            // Arrange
            string email = null;
            string fullName = "Invalid User";

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _authService.HandleExternalLoginAsync(email, fullName));
            _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleExternalLoginAsync_ShouldRollback_WhenCreationFails()
        {
            // Arrange
            var email = "failcreate@external.com";
            var fullName = "Failing User";

            var transactionMock = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "External creation failed." }));

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _authService.HandleExternalLoginAsync(email, fullName));

            // Verify Rollback
            transactionMock.Verify(t => t.RollbackAsync(default), Times.AtMost(2));
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion
    }
}*/