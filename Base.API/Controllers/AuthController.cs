using Azure.Core;
using Base.API.DTOs;
using Base.Services.Implementations;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Base.Shared.Responses;
using Base.Shared.Responses.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Base.API.Controllers
{
    [ApiController]
    [Route("api/Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserProfileService _userProfile;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IUserProfileService userProfile)
        {
            _authService = authService;
            _logger = logger;
            _userProfile = userProfile;
        }

        #region Login
        /// <summary>
        /// Authenticates a user and generates authentication tokens if successful.
        /// </summary>
        /// <remarks>
        /// This endpoint handles user login attempts. It validates the provided credentials and returns appropriate responses based on the authentication outcome.
        /// 
        /// Possible scenarios:
        /// - If the email is not confirmed, an OTP is sent for verification, and the response indicates that confirmation is required.
        /// - If two-factor authentication (2FA) is enabled, an OTP is sent for login, and the response prompts for OTP verification.
        /// - If credentials are invalid or the account is locked, an error message is returned.
        /// - On successful login without additional verification, JWT and refresh tokens are provided along with user details.
        /// 
        /// The response body is always a <see cref="LoginResult"/> object containing success status, messages, error codes, and conditional sections (Auth, Verification, User).
        /// 
        /// Example Response (Full Success - 200):
        /// {
        ///   "Success": true,
        ///   "Message": "Login successful.",
        ///   "ErrorCode": null,
        ///   "Auth": {
        ///     "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///     "RefreshToken": "abc123-refresh",
        ///     "TokenExpiry": "2025-11-19T14:00:00Z"
        ///   },
        ///   "Verification": {
        ///     "RequiresOtpVerification": false,
        ///     "EmailConfirmed": true
        ///   },
        ///   "User": {
        ///     "Id": "user123",
        ///     "UserName": "john_doe",
        ///     "Email": "john@example.com",
        ///     "UserType": "Admin",
        ///     "Roles": ["Admin", "User"]
        ///   }
        /// }
        /// 
        /// Example Response (OTP Required - 202):
        /// {
        ///   "Success": true,
        ///   "Message": "OTP sent for login.",
        ///   "ErrorCode": null,
        ///   "Auth": null,
        ///   "Verification": {
        ///     "RequiresOtpVerification": true,
        ///     "EmailConfirmed": true,
        ///     "Email": "john@example.com"
        ///   },
        ///   "User": null
        /// }
        /// 
        /// Example Response (Invalid Credentials - 401):
        /// {
        ///   "Success": false,
        ///   "Message": "Invalid credentials.",
        ///   "ErrorCode": "INVALID_CREDENTIALS",
        ///   "Auth": null,
        ///   "Verification": null,
        ///   "User": null
        /// }
        /// </remarks>
        /// <param name="model">The login credentials including email and password.</param>
        /// <response code="200">Full login success with tokens and user details. Returns <see cref="LoginResult"/>.</response>
        /// <response code="202">Login requires further verification (e.g., OTP or email confirmation). Returns <see cref="LoginResult"/> with verification details.</response>
        /// <response code="400">Invalid model state (e.g., missing or malformed input).</response>
        /// <response code="401">Invalid credentials. Returns <see cref="LoginResult"/> with error details.</response>
        /// <response code="403">Account locked. Returns <see cref="LoginResult"/> with error details.</response>
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status403Forbidden)]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.LoginUserAsync(model);

            if (!result.Success)
            {
                return result.ErrorCode == "ACCOUNT_LOCKED" ? StatusCode(403, result) : Unauthorized(result);
            }

            if (result.Verification?.RequiresOtpVerification == true || result.Verification?.EmailConfirmed == false)
            {
                return Accepted(result); // 202 Accepted for partial success
            }

            return Ok(result); // 200 OK for full success
        }

        /// <summary>
        /// Verifies the OTP for login and generates authentication tokens if successful.
        /// </summary>
        /// <remarks>
        /// This endpoint handles OTP verification for the login process (e.g., for 2FA). It validates the provided OTP and returns appropriate responses based on the verification outcome.
        /// 
        /// Possible scenarios:
        /// - If the OTP is invalid, an error message is returned.
        /// - If the user is not found or the account is locked, an error message is returned.
        /// - On successful verification, JWT and refresh tokens are provided along with user details.
        /// 
        /// The response body is always a <see cref="LoginResult"/> object containing success status, messages, error codes, and conditional sections (Auth, Verification, User).
        /// 
        /// Example Response (Success - 200):
        /// {
        ///   "Success": true,
        ///   "Message": "Verification successful.",
        ///   "ErrorCode": null,
        ///   "Auth": {
        ///     "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///     "RefreshToken": "abc123-refresh",
        ///     "TokenExpiry": "2025-11-19T14:00:00Z"
        ///   },
        ///   "Verification": {
        ///     "RequiresOtpVerification": false,
        ///     "EmailConfirmed": true
        ///   },
        ///   "User": {
        ///     "Id": "user123",
        ///     "UserName": "john_doe",
        ///     "Email": "john@example.com",
        ///     "UserType": "Admin",
        ///     "Roles": ["Admin", "User"]
        ///   }
        /// }
        /// 
        /// Example Response (Invalid OTP - 401):
        /// {
        ///   "Success": false,
        ///   "Message": "Invalid OTP.",
        ///   "ErrorCode": "INVALID_OTP",
        ///   "Auth": null,
        ///   "Verification": null,
        ///   "User": null
        /// }
        /// 
        /// Example Response (Account Locked - 403):
        /// {
        ///   "Success": false,
        ///   "Message": "Account locked.",
        ///   "ErrorCode": "ACCOUNT_LOCKED",
        ///   "Auth": null,
        ///   "Verification": null,
        ///   "User": null
        /// }
        /// </remarks>
        /// <param name="model">The OTP verification details including email and OTP.</param>
        /// <response code="200">OTP verification successful with tokens and user details. Returns <see cref="LoginResult"/>.</response>
        /// <response code="400">Invalid model state (e.g., missing or malformed input).</response>
        /// <response code="401">Invalid OTP or user not found. Returns <see cref="LoginResult"/> with error details.</response>
        /// <response code="403">Account locked. Returns <see cref="LoginResult"/> with error details.</response>
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status403Forbidden)]
        [HttpPost("login/verify")]
        public async Task<IActionResult> VerifyLogin([FromBody] VerifyOtpDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.VerifyLoginAsync(model);

            if (!result.Success)
            {
                if (result.ErrorCode == "ACCOUNT_LOCKED") return StatusCode(403, result);
                return Unauthorized(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Logs out the authenticated user by revoking all refresh tokens.
        /// </summary>
        /// <remarks>
        /// This endpoint requires authentication. It revokes all active refresh tokens for the user, effectively logging them out from all sessions.
        /// 
        /// Possible scenarios:
        /// - If the user ID is invalid or not found, an error message is returned.
        /// - On successful logout, a confirmation message is provided.
        /// 
        /// The response body is always an <see cref="ApiResponse"/> object containing success status, messages, and error codes if applicable.
        /// 
        /// Example Response (Success - 200):
        /// {
        ///   "Success": true,
        ///   "Message": "Logged out successfully.",
        ///   "ErrorCode": null
        /// }
        /// 
        /// Example Response (Invalid User ID - 401):
        /// {
        ///   "Success": false,
        ///   "Message": "Invalid user ID.",
        ///   "ErrorCode": "INVALID_USER_ID"
        /// }
        /// 
        /// Example Response (Unauthorized - 401):
        /// {
        ///   "Success": false,
        ///   "Message": "Unauthorized access.",
        ///   "ErrorCode": "UNAUTHORIZED"
        /// }
        /// </remarks>
        /// <response code="200">Logout successful. Returns <see cref="ApiResponse"/> with confirmation.</response>
        /// <response code="401">Unauthorized access, invalid user ID, or user not found. Returns <see cref="ApiResponse"/> with error details.</response>
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized access.", ErrorCode = "UNAUTHORIZED" });

            var result = await _authService.LogoutAsync(userId);
            if (!result.Success)
            {
                return Unauthorized(result); // Or BadRequest if appropriate, but 401 fits for auth issues
            }
            return Ok(result);
        }


        /// <summary>
        /// Resends OTP for login purpose
        /// </summary>
        /// <remarks>
        /// This endpoint allows the user to request a new OTP during login if the previous one expired or was not received.
        /// 
        /// Privacy note: The same success response is returned whether the email exists or not to prevent enumeration attacks.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/login/resend-otp?email=user@example.com
        /// 
        /// </remarks>
        /// <param name="email">User's email address</param>
        /// <response code="202">OTP sent successfully (or would have been sent if email exists)</response>
        /// <response code="400">Invalid email format</response>
        /// <response code="429">Too many resend requests</response>
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status429TooManyRequests)]
        [HttpPost("login/resend-otp")]
        public async Task<IActionResult> ResendLoginOtp([FromQuery] string email)
        {
            var result = await _authService.ResendOtpAsync(email,"login");

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        #endregion

        #region Registration & Email
        // <response code="429">Too many registration attempts from this IP</response>
        //[ProducesResponseType(typeof(RegisterResult), StatusCodes.Status429TooManyRequests)]

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <remarks>
        /// Creates a new user and sends a 6-digit verification code to the provided email.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/register
        ///     {
        ///       "email": "user@example.com",
        ///       "password": "StrongP@ssw0rd",
        ///       "fullName": "Ahmed Mohamed",
        ///       "phoneNumber": "+201234567890"
        ///     }
        /// 
        /// </remarks>
        /// <response code="201">Account created successfully. Check your email for verification code.</response>
        /// <response code="400">Validation errors or email already exists</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.RegisterAsync(model, ip, userAgent);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "TOO_MANY_REQUESTS" => StatusCode(StatusCodes.Status429TooManyRequests, result),
                    "EMAIL_EXISTS" => Conflict(result), // 409 أفضل من 400 لهذه الحالة
                    _ => BadRequest(result)
                };
            }

            // 201 Created → لأننا أنشأنا مورد جديد (المستخدم)
            return CreatedAtAction(nameof(Register), result);
        }

        /// <summary>
        /// Verify user email with 6-digit code
        /// </summary>
        /// <remarks>
        /// Completes the email verification process after registration.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/email/verify
        ///     {
        ///       "email": "user@example.com",
        ///       "code": "483920"
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">Email verified successfully</response>
        /// <response code="400">Invalid, expired, or too many attempts</response>
        /// <response code="409">Email already verified</response>
        [HttpPost("email/verify")]
        [ProducesResponseType(typeof(VerifyEmailResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(VerifyEmailResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(VerifyEmailResult), StatusCodes.Status409Conflict)]
        [Produces("application/json")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyOtpDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.VerifyEmailAsync(request);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "ALREADY_VERIFIED" => Conflict(result), // 409 Conflict
                    "TOO_MANY_ATTEMPTS" => BadRequest(result),
                    _ => BadRequest(result)
                };
            }

            return Ok(result);
        }

        /// <summary>
        /// Resend email verification OTP
        /// </summary>
        /// <remarks>
        /// Allows the user to request a new email verification OTP if the previous one expired or was not received.
        /// 
        /// **Security Note**: The same success response is returned regardless of whether the email exists 
        /// to prevent user enumeration attacks.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/email/resend-otp?email=user@example.com
        /// 
        /// </remarks>
        /// <param name="email">The user's email address</param>
        /// <response code="202">OTP sent (or would have been sent if the email is registered)</response>
        /// <response code="400">Invalid email format</response>
        /// <response code="429">Too many requests - rate limited</response>
        [HttpPost("email/resend-otp")]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status429TooManyRequests)]
        [Produces("application/json")]
        public async Task<IActionResult> ResendVerification([FromQuery] string email)
        {
            var result = await _authService.ResendOtpAsync(email,"verifyemail");
            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }
        #endregion

        #region Password Recovery
        /// <summary>
        /// Initiate password reset by sending OTP to email
        /// </summary>
        /// <remarks>
        /// Sends a 6-digit OTP to the user's email if the account exists.
        /// 
        /// **Important Security Note**: Always returns success to prevent user enumeration attacks.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/password/forgot
        ///     { "email": "user@example.com" }
        /// 
        /// </remarks>
        /// <response code="202">Reset code sent (or would be sent if email exists)</response>
        /// <response code="400">Invalid email format</response>
        /// <response code="429">Too many requests</response>
        [ProducesResponseType(typeof(ForgotPasswordResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ForgotPasswordResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ForgotPasswordResult), StatusCodes.Status429TooManyRequests)]
        [HttpPost("password/forgot")]
        public async Task<IActionResult> ForgotPassword([FromQuery] string email)
        {
            var result = await _authService.ForgotPasswordAsync(email);

            if (!result.Success)
            {
                return result.ErrorCode switch
                {
                    "RESEND_COOLDOWN" or "TOO_MANY_RESENDS" =>
                        StatusCode(StatusCodes.Status429TooManyRequests, result),
                    _ => BadRequest(result)
                };
            }

            // 202 Accepted = "تم قبول الطلب، لكن في خطوة إضافية مطلوبة (إدخال الـ OTP)"
            return Accepted(result);
        }

        /// <summary>
        /// Verify password reset OTP and issue a reset token
        /// </summary>
        /// <remarks>
        /// Verifies the 6-digit code sent to the user's email and returns a short-lived reset token 
        /// that can be used to change the password.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/password/verifyotp
        ///     {
        ///       "email": "user@example.com",
        ///       "otp": "283947"
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">OTP verified successfully. Use ResetToken to change password.</response>
        /// <response code="400">Invalid, expired, or blocked OTP</response>
        [ProducesResponseType(typeof(VerifyResetOtpResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(VerifyResetOtpResult), StatusCodes.Status400BadRequest)]
        [HttpPost("password/verifyotp")]
        public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyOtpDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new VerifyResetOtpResult { Success = false, Message = "Invalid request format." });
            var result = await _authService.VerifyOtpAndGenerateResetTokenAsync(model);

            if (!result.Success)
                return BadRequest(result);

            // 200 OK لأن العملية اكتملت بنجاح (خلافاً للـ resend اللي بيبقى 202)
            return Ok(result);
        }

        /// <summary>
        /// Reset user password using reset token
        /// </summary>
        /// <remarks>
        /// Final step in the password reset flow.
        /// Requires the 6-digit code sent to email + the reset token from /password/verifyotp
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/password/reset
        ///     {
        ///       "email": "user@example.com",
        ///       "resetToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.xxxxx",
        ///       "newPassword": "NewStrongP@ssw0rd123!"
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">Password changed successfully</response>
        /// <response code="400">Invalid code, expired link, or too many attempts</response>
        [HttpPost("password/reset")]
        [ProducesResponseType(typeof(ResetPasswordResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResetPasswordResult), StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(model);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Resend Password OTP
        /// </summary>
        /// <remarks>
        /// Allows the user to request a new Password OTP if the previous one expired or was not received.
        /// 
        /// **Security Note**: The same success response is returned regardless of whether the email exists 
        /// to prevent user enumeration attacks.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/password/resend-otp?email=user@example.com
        /// 
        /// </remarks>
        /// <param name="email">The user's email address</param>
        /// <response code="202">OTP sent (or would have been sent if the email is registered)</response>
        /// <response code="400">Invalid email format</response>
        /// <response code="429">Too many requests - rate limited</response>
        [HttpPost("password/resend-otp")]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status429TooManyRequests)]
        [Produces("application/json")]
        public async Task<IActionResult> ResendPasswordOtp([FromQuery] string email)
        {
            var result = await _authService.ResendOtpAsync(email, "reset");

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="Base.Services.Implementations.BadRequestException">
        /// The current user could not be located.
        /// or
        /// An unexpected error occurred.
        /// </exception>
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [HttpPost("password/change")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid request data.", "VALIDATION_ERROR"));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("Invalid or missing authentication token.", "INVALID_TOKEN"));

            var result = await _authService.ChangePasswordAsync(userId, model);

            return result.Success
                ? Ok(ApiResponse.Ok(result.Message))
                : BadRequest(ApiResponse.Fail(result.Message, result.ErrorCode));
        }
        #endregion

        #region 2FA
        /// <summary>
        /// used to initiate the 2FA enabling process
        /// </summary>
        /// <returns></returns>
        [HttpPost("2fa/initiate")]
        [Authorize]
        public async Task<IActionResult> Initiate2Fa()
        {
            var Email =  User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(Email))
                return Unauthorized(ApiResponse.Fail("Invalid token."));

            var result = await _authService.InitiateEnable2FaAsync(Email);
            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        /// <summary>
        /// used to confirm enabling 2FA with the provided OTP
        /// </summary>
        [HttpPost("2fa/confirm")]
        [Authorize]
        public async Task<IActionResult> Confirm2Fa([FromQuery] string Otp)
        {
            if (string.IsNullOrEmpty(Otp))
                return BadRequest(ApiResponse.Fail("Invalid request.", "VALIDATION_ERROR"));

            var Email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(Email))
                return Unauthorized(ApiResponse.Fail("Invalid token."));

            var result = await _authService.ConfirmEnable2FaAsync(Email, Otp);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        /// <summary>
        /// used to disable 2FA for the user
        /// </summary>
        [HttpPost("2fa/disable")]
        [Authorize]
        public async Task<IActionResult> Disable2Fa([FromBody] Disable2FaDTO model)
        {
            if(!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Invalid request.", "VALIDATION_ERROR"));

            var Email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(Email))
                return Unauthorized(ApiResponse.Fail("Invalid token."));

            var result = await _authService.Disable2FaAsync(Email, model.CurrentPassword);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        /// <summary>
        /// Resend Two Factor Authentication OTP
        /// </summary>
        /// <remarks>
        /// Allows the user to request a new Two Factor Authentication OTP if the previous one expired or was not received.
        /// 
        /// **Security Note**: The same success response is returned regardless of whether the email exists 
        /// to prevent user enumeration attacks.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/2fa/resend-otp
        /// 
        /// </remarks>
        /// <response code="202">OTP sent (or would have been sent if the email is registered)</response>
        /// <response code="400">Invalid email format</response>
        /// <response code="429">Too many requests - rate limited</response>
        [HttpPost("2fa/resend-otp")]
        [Authorize]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResendOtpResult), StatusCodes.Status429TooManyRequests)]
        [Produces("application/json")]
        public async Task<IActionResult> Resend2FaOtp()
        {
            var Email = User.FindFirstValue(ClaimTypes.Email);
            var result = await _authService.ResendOtpAsync(Email, "2fa");

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        #endregion

        #region refreshtoken
        /// <summary>
        /// Refresh JWT token using a valid refresh token
        /// </summary>
        /// <remarks>
        /// Implements secure refresh token rotation with reuse detection and automatic revocation on compromise.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/auth/token/refresh
        ///     {
        ///       "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.xxxxx"
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">New access & refresh tokens issued</response>
        /// <response code="401">Invalid, expired, or revoked refresh token</response>
        [HttpPost("token/refresh")]
        [ProducesResponseType(typeof(RefreshTokenResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RefreshTokenResult), StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RefreshTokenAsync(request);

            return result.Success
                ? Ok(result)
                : Unauthorized(result); // 401 دائمًا لأي فشل في الـ refresh
        }
        #endregion

        #region me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("Invalid or missing authentication token.", "INVALID_TOKEN"));

            var profile = await _userProfile.GetByIdAsync(userId);

            if (profile is null)
                return NotFound(ApiResponse.NotFound("User profile not found."));

            return Ok(ApiResponse.Ok(data: profile));
        }
        #endregion
    }
}
