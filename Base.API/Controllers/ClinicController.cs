using Azure.Core;
using Base.API.DTOs;
using Base.API.Helper;
using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Base.Services.Implementations;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Base.Shared.Responses.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ActiveUserOnly")]

    public class ClinicController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly IUploadImageService _uploadImageService;

        public ClinicController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IEmailSender emailSender, IUploadImageService uploadImageService)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _uploadImageService = uploadImageService;
        }

        /// <summary>
        /// Handles the creation of a new clinic registration request.
        /// </summary>
        /// <remarks>This method validates the provided registration details and ensures that no existing
        /// clinic is registered  with the same email address. If the registration is valid and unique, the clinic
        /// registration request is  saved and processed for review. An email notification will be sent after the review
        /// process is completed.</remarks>
        /// <param name="model">The clinic registration details provided in the request body. This must be a valid  <see
        /// cref="ClincRegistrationDTO"/> object containing the necessary information for registration.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. If the request is successful, 
        /// returns an HTTP 200 response with a message confirming that the request has been received and is under
        /// review.</returns>
        /// <exception cref="BadRequestException">Thrown if the provided registration details are invalid, if a clinic with the same email address already
        /// exists,  or if an error occurs during the registration process.</exception>
        [HttpPost("create-clinicrequest")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateClinic([FromBody] ClincRegistrationDTO model)
        {
            try
            {

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    throw new BadRequestException(errors);
                }

                var MedicalSpecialtyRepo = _unitOfWork.Repository<MedicalSpecialty>();
                var MedicalSpecialtyspec = new BaseSpecification<MedicalSpecialty>(c => c.Id == model.MedicalSpecialtyId);
                var count = await MedicalSpecialtyRepo.CountAsync(MedicalSpecialtyspec);
                if (count == 0) throw new BadRequestException("This Medical Specialty is not exist");

                var ClincRepo = _unitOfWork.Repository<Clinic>();
                var spec = new BaseSpecification<Clinic>(c => c.Email.ToLower() == model.Email.ToLower());
                var result = (await ClincRepo.CountAsync(spec)) > 0 || (await _userManager.FindByEmailAsync(model.Email) is not null);
                if (result) throw new BadRequestException("A clinic with this email already exists.");

                var _Clinc = model.ToClinc();
                await ClincRepo.AddAsync(_Clinc);
                if (await _unitOfWork.CompleteAsync() > 0)
                    return Ok(new
                    {
                        message = "We have received your request, and it is currently under review. An email will be sent after the review."
                    });

                throw new BadRequestException("Failed to create Clinic Request");
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Log the exception
                throw new InternalServerException("An unexpected internal error occurred during create clinic request.");
            }
        }

        /// <summary>
        /// Gets the clinics requests.
        /// </summary>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="valueToSearch">The value to search.</param>
        /// <param name="pageIndex">Index of the page.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        /// <exception cref="Base.Services.Implementations.NotFoundException">No Clinc requests are currently defined in the system.</exception>
        [HttpGet("clinics-requests")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> GetClinicsRequests(string? searchType = null, string? valueToSearch = null, int pageIndex = 1, int pageSize = 10)
        {
            Expression<Func<Clinic, bool>> CriteriaExpression;

            if (!string.IsNullOrEmpty(searchType) && !string.IsNullOrEmpty(valueToSearch))
            {
                if (Enum.TryParse<ClinicSearchType>(searchType, true, out var searchTypeEnum))
                {
                    switch (searchTypeEnum)
                    {
                        case ClinicSearchType.Name:
                            CriteriaExpression = c => c.Status.ToLower() == ClinicStatus.pending.ToString() && c.Name.ToLower().Contains(valueToSearch.ToLower());
                            break;
                        case ClinicSearchType.Email:
                            CriteriaExpression = c => c.Status.ToLower() == ClinicStatus.pending.ToString() && c.Email.ToLower().Contains(valueToSearch.ToLower());
                            break;
                        case ClinicSearchType.Status:
                            CriteriaExpression = c => c.Status.ToLower() == valueToSearch.ToLower();
                            break;
                        default:
                            CriteriaExpression = c => c.Status.ToLower() == ClinicStatus.pending.ToString();
                            break;
                    }
                }
                else
                {
                    CriteriaExpression = c => c.Status.ToLower() == ClinicStatus.pending.ToString();
                }
            }
            else
            {
                CriteriaExpression = c => c.Status.ToLower() == ClinicStatus.pending.ToString();
            }
            var result = await GetClinicsAsync(CriteriaExpression, pageIndex, pageSize);
            if (!result.list.Any())
            {
                throw new NotFoundException("No Clinc requests are currently defined in the system.");
            }
            return Ok(new { message = "All Requests", result });
        }

        /// <summary>
        /// Approves a clinic's request to join the system by updating its status to "active" and creating an associated
        /// clinic administrator account if one does not already exist.
        /// </summary>
        /// <remarks>This method performs the following actions: <list type="bullet"> <item>Validates the
        /// provided <paramref name="ClincId"/> and ensures the clinic exists in the system.</item> <item>Updates the
        /// clinic's status to "active".</item> <item>Creates a clinic administrator account if one does not already
        /// exist for the clinic.</item> <item>Sends an email notification to the clinic administrator with their
        /// account details.</item> </list> The method requires the caller to have the "SystemAdmin" role and is
        /// accessible via an HTTP PATCH request.</remarks>
        /// <param name="ClincId">The unique identifier of the clinic to be approved. This parameter cannot be null or empty.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns a success response if the
        /// clinic is approved and the administrator account is created successfully.</returns>
        /// <exception cref="BadRequestException">Thrown if <paramref name="ClincId"/> is null, empty, or if the operation fails due to invalid input or email
        /// sending issues.</exception>
        /// <exception cref="NotFoundException">Thrown if no clinic is found with the specified <paramref name="ClincId"/>.</exception>
        [HttpPatch("approve-clinic-request")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> ApproveClinicsRequests(string ClinicId)
        {
            if (string.IsNullOrEmpty(ClinicId)) throw new BadRequestException("ClinicId is required");

            var ClincRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => c.Id == ClinicId);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            var request = await ClincRepo.GetEntityWithSpecAsync(spec);

            if (request is null) throw new NotFoundException("ClinicId not found");
            request.Status = ClinicStatus.active.ToString();
            await ClincRepo.UpdateAsync(request);
            if (await _unitOfWork.CompleteAsync() > 0)
            {
                // ✅ تحقق لو الأدمن مش 
                var ClincadminUser = await _userManager.FindByEmailAsync(request.Email);
                if (ClincadminUser == null)
                {
                    ClincadminUser = new ApplicationUser
                    {
                        FullName = request.Name,
                        Type = UserTypes.ClinicAdmin,
                        //UserType = "ClincAdmin",
                        UserName = request.Email,
                        Email = request.Email,
                        EmailConfirmed = true,
                        ClincAdminProfile = new ClincAdminProfile()
                        {
                            ClincId = request.Id,
                        }
                    };

                    var password = GeneratePassword();

                    var result = await _userManager.CreateAsync(ClincadminUser, password);
                    if (result.Succeeded)
                    {

                        var res = await _userManager.AddToRoleAsync(ClincadminUser, UserTypes.ClinicAdmin.ToString());
                        try
                        {
                            await _emailSender.SendEmailAsync(request.Email, "Clinic Add Request Acceptance",
                                ApproveClinicsRequestsMail(request.Name, request.Email, password));
                        }
                        catch (Exception ex)
                        {
                            throw new BadRequestException("Failed to send mail");
                        }
                        return Ok(new { message = "Clinic Now Available in System" });
                    }
                }
            }

            throw new BadRequestException("Failed to Approve Clinc Request");
        }

        [HttpPatch("reject-clinic-request")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> RejectClinicsRequests(string ClinicId)
        {
            if (string.IsNullOrEmpty(ClinicId)) throw new BadRequestException("ClinicId is required");

            var ClincRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => c.Id == ClinicId);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            var request = await ClincRepo.GetEntityWithSpecAsync(spec);

            if (request is null) throw new NotFoundException("ClinicId not found");
            request.Status = ClinicStatus.notactive.ToString();
            await ClincRepo.UpdateAsync(request);
            if (await _unitOfWork.CompleteAsync() > 0)
                return Ok(new { message = "Request rejected successfully" });


            throw new BadRequestException("Failed to Reject Clinic Request");
        }

        /// <summary>
        /// Gets the system clinics.
        /// </summary>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="valueToSearch">The value to search.</param>
        /// <param name="pageIndex">Index of the page.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        /// <exception cref="Base.Services.Implementations.NotFoundException">No Clincs are currently defined in the system.</exception>
        [HttpGet("system-clinics")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> GetSystemClinics(string? searchType = null, string? valueToSearch = null, int pageIndex = 1, int pageSize = 10)
        {
            Expression<Func<Clinic, bool>> CriteriaExpression;

            if (!string.IsNullOrEmpty(searchType) && !string.IsNullOrEmpty(valueToSearch))
            {
                if (Enum.TryParse<ClinicSearchType>(searchType, true, out var searchTypeEnum))
                {
                    switch (searchTypeEnum)
                    {
                        case ClinicSearchType.Name:
                            CriteriaExpression = c => c.Status.ToLower() != ClinicStatus.pending.ToString() && c.Name.ToLower().Contains(valueToSearch.ToLower());
                            break;
                        case ClinicSearchType.Email:
                            CriteriaExpression = c => c.Status.ToLower() != ClinicStatus.pending.ToString() && c.Email.ToLower().Contains(valueToSearch.ToLower());
                            break;
                        case ClinicSearchType.Status:
                            CriteriaExpression = c => c.Status.ToLower() == valueToSearch.ToLower();
                            break;
                        default:
                            CriteriaExpression = c => c.Status.ToLower() != ClinicStatus.pending.ToString();
                            break;
                    }
                }
                else
                {
                    CriteriaExpression = c => c.Status.ToLower() != ClinicStatus.pending.ToString();
                }
            }
            else
            {
                CriteriaExpression = c => c.Status.ToLower() != ClinicStatus.pending.ToString();
            }

            var result = await GetClinicsAsync(CriteriaExpression, pageIndex, pageSize);
            if (!result.list.Any())
            {
                throw new NotFoundException("No Clincs are currently defined in the system.");
            }
            return Ok(new { message = "All Requests", result });
        }

        /// <summary>
        /// Activates a clinic by setting its status to active.
        /// </summary>
        /// <param name="ClincId">The unique identifier of the clinic to activate. Cannot be null or empty.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.  Returns a 200 OK response with a
        /// success message if the clinic is successfully activated.</returns>
        /// <exception cref="BadRequestException">Thrown if <paramref name="ClincId"/> is null or empty.</exception>
        /// <exception cref="NotFoundException">Thrown if the activation process fails, indicating the clinic could not be found or updated.</exception>
        [HttpPatch("activate-Clinic")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> ActivateClinic(string ClinicId)
        {
            try
            {
                if (string.IsNullOrEmpty(ClinicId)) throw new BadRequestException("ClinicId is Required");
                var result = await ChangeClinicStatusAsync(c => c.Id == ClinicId, ClinicStatus.active);
                if (!result) throw new NotFoundException("Faild To Activate Clinic");

                return Ok(new { message = "Clinic Activated" });
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Log the exception
                throw new InternalServerException("An unexpected error occurred during Activate Clinic. Please try again.");
            }
        }

        /// <summary>
        /// Deactivates a clinic by setting its status to "not active."
        /// </summary>
        /// <remarks>This endpoint is restricted to users with the "SystemAdmin" role.</remarks>
        /// <param name="ClincId">The unique identifier of the clinic to deactivate. Cannot be null or empty.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.  Returns a 200 OK response with a
        /// success message if the clinic is successfully deactivated.</returns>
        /// <exception cref="BadRequestException">Thrown if <paramref name="ClincId"/> is null or empty.</exception>
        /// <exception cref="NotFoundException">Thrown if the operation fails to deactivate the clinic.</exception>
        [HttpPatch("deactivate-clinic")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> DeactivateClinic(string ClinicId)
        {
            try
            {
                if (string.IsNullOrEmpty(ClinicId)) throw new BadRequestException("ClinicId is Required");
                var result = await ChangeClinicStatusAsync(c => c.Id == ClinicId, ClinicStatus.notactive);
                if (!result) throw new NotFoundException("Faild To Deactivate Clinic");

                return Ok(new { message = "Clinic Deactivated" });
            }
            catch (Exception ex)
            {

                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Log the exception
                throw new InternalServerException("An unexpected error occurred during Deactivate Clinic. Please try again.");
            }
        }

        /// <summary>
        /// Retrieves a list of clinic administrators for the specified clinic.
        /// </summary>
        /// <remarks>This endpoint is restricted to users with the "SystemAdmin" role. Ensure the provided
        /// <paramref name="ClincId"/>  corresponds to a valid clinic.</remarks>
        /// <param name="ClincId">The unique identifier of the clinic whose administrators are to be retrieved.</param>
        /// <returns>An <see cref="IActionResult"/> containing an <see cref="ApiResponseDTO"/> with a status code of 200  and a
        /// collection of clinic administrators. Each administrator is represented by their user ID and full name.</returns>
        /// <exception cref="NotFoundException">Thrown if no administrators are defined for the specified clinic.</exception>
        [HttpGet("clinic-adminusers")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> GetClinicAdmins(string ClinicId)
        {
            var Repo = _unitOfWork.Repository<ClincAdminProfile>();
            var spec = new BaseSpecification<ClincAdminProfile>(e => e.ClincId == ClinicId);
            var result = (await Repo.ListAsync(spec)).Select(e => new ClinicAdminDTO { userId = e.User?.Id, FullName = e.User?.FullName }).ToList();
            if (!result.Any())
            {
                throw new NotFoundException("No Admin are currently defined in this Clinic.");
            }
            return Ok(result);
        }

        /// <summary>
        /// Resets the password for a clinic administrator account.
        /// </summary>
        /// <remarks>This method is accessible only to users with the "SystemAdmin" role. It validates the
        /// provided input, resets the password for the specified clinic administrator, and sends an email notification
        /// with the new password. If the email fails to send, the password reset is still considered successful, but an
        /// error message is returned.</remarks>
        /// <param name="model">An instance of <see cref="ClincAdminResetPasswordDTO"/> containing the administrator's ID and the new
        /// password.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns a success response if the
        /// password is reset successfully, or an appropriate error response if the operation fails.</returns>
        /// <exception cref="BadRequestException">Thrown if the input model is invalid, the password reset operation fails, or the email notification cannot
        /// be sent.</exception>
        /// <exception cref="NotFoundException">Thrown if no user is found with the specified administrator ID.</exception>
        /// <exception cref="InternalServerException">Thrown if an unexpected error occurs during the password reset process.</exception>

        [HttpPost("resetpassword-foruser")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> ResetPasswordforClinicAdmin([FromBody] ClincAdminResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.AdminId);
                if (user is null)
                {
                    throw new NotFoundException($"There is no user with UserId '{model.AdminId}'");
                }
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    throw new BadRequestException($"Reset Password failed: {string.Join(", ", errors)}");

                }
                try
                {

                    await _emailSender.SendEmailAsync(user.Email, "New Password for your Account",
                        GetPasswordResetTemplate(user.FullName, user.Email, model.NewPassword, "", "", "", "", DateTime.Now.Year));
                }
                catch (Exception ex)
                {
                    throw new BadRequestException("Password Reseted Successfully but Failed to send mail");
                }
                // 3. Success response
                return Ok(new { message = "Password Reset successfully." });
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                throw new InternalServerException("An unexpected error occurred during Reset Password");
            }
        }

        /// <summary>
        /// Creates a new clinic administrator account and assigns the user to the "ClincAdmin" role.
        /// </summary>
        /// <remarks>This method validates the input model, ensures the email does not already exist, and
        /// creates a new user with the "ClincAdmin" role.  The user's email is confirmed by default, and a randomly
        /// generated password is assigned.  An email is sent to the user with their account details. If the email fails
        /// to send, the user is still created, but an exception is thrown.</remarks>
        /// <param name="model">The data transfer object containing the details required to create the clinic administrator, including their
        /// full name, email, and associated clinic ID.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. If successful, returns an HTTP 200
        /// response with a message confirming the creation of the clinic administrator.</returns>
        /// <exception cref="BadRequestException">Thrown if the input model is invalid, the email already exists, the user creation fails, or the email
        /// notification cannot be sent.</exception>
        [HttpPost("create-clinicadmin")]
        [Authorize(Roles = nameof(UserTypes.SystemAdmin))]
        public async Task<IActionResult> CreateClinicAdmin([FromBody] ClincAdminProfileCreateDTO model)
        {
            try
            {

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    throw new BadRequestException(errors);
                }
                var checkEmailExsitClincRepo = _unitOfWork.Repository<Clinic>();
                var checkEmailExsitspec = new BaseSpecification<Clinic>(c => c.Email.ToLower() == model.Email.ToLower());
                var checkEmailExsits = (await checkEmailExsitClincRepo.CountAsync(checkEmailExsitspec)) > 0 || (await _userManager.FindByEmailAsync(model.Email) is not null);
                if (checkEmailExsits) throw new BadRequestException("This email is already registered.");

                var ClincadminUser = new ApplicationUser
                {
                    FullName = model.FullName,
                    //UserType = "ClincAdmin",
                    Type = UserTypes.ClinicAdmin,
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    ClincAdminProfile = new ClincAdminProfile()
                    {
                        ClincId = model.ClincId,
                    }
                };

                var password = GeneratePassword();
                var result = await _userManager.CreateAsync(ClincadminUser, password);
                if (!result.Succeeded)
                {
                    throw new BadRequestException("Faild to Create User");
                }
                await _userManager.AddToRoleAsync(ClincadminUser, UserTypes.ClinicAdmin.ToString());
                try
                {
                    var clincrepo = _unitOfWork.Repository<Clinic>();
                    var spec = new BaseSpecification<Clinic>(c => c.Id == model.ClincId);
                    var clinic = await clincrepo.GetEntityWithSpecAsync(spec);
                    await _emailSender.SendEmailAsync(model.Email, "your clinic admin account",
                        GetClinicAdminAccountCreatedTemplate(model.FullName, clinic.Name, model.Email, password, "", "", "", "", DateTime.Now.Year));
                }
                catch (Exception ex)
                {
                    throw new BadRequestException("User Created Successfully but Failed to send mail");
                }
                return Ok(new { message = $"'{model.FullName}' is Now Admin for ClinicId '{model.ClincId}'" });

            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Log the exception
                throw new InternalServerException("An unexpected internal error occurred during create clinic Admin.");
            }

        }

        #region Helper Method
        public static string GeneratePassword(int length = 12)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()-_=+[]{}|;:,.<>?";

            string allChars = upper + lower + digits + special;
            StringBuilder password = new StringBuilder();
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            // Ensure password contains at least one of each required character type
            password.Append(GetRandomChar(upper, rng));
            password.Append(GetRandomChar(lower, rng));
            password.Append(GetRandomChar(digits, rng));
            password.Append(GetRandomChar(special, rng));

            // Fill remaining characters
            for (int i = password.Length; i < length; i++)
            {
                password.Append(GetRandomChar(allChars, rng));
            }

            // Shuffle the result for randomness
            return new string(password.ToString().OrderBy(_ => Guid.NewGuid()).ToArray());
        }

        private static char GetRandomChar(string charset, RandomNumberGenerator rng)
        {
            byte[] buffer = new byte[4];
            rng.GetBytes(buffer);
            int value = BitConverter.ToInt32(buffer, 0);
            return charset[Math.Abs(value) % charset.Length];
        }

        private static string ApproveClinicsRequestsMail(string ClincName, string username, string password)
        {
            string systemName = "Clinc Management System";
            string loginUrl = "https://your-system-url.com/login";
            string supportEmail = "support@your-system.com";
            string supportPhone = "+20 100 000 0000";
            string passwordExpiryDate = DateTime.Now.AddDays(1).ToString("MMMM dd, yyyy");

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width,initial-scale=1' />
  <title>Clinc Registration Approved - {ClincName}</title>
  <style>
    body {{ font-family: Arial, sans-serif; direction: ltr; color: #222; }}
    .container {{ max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #eee; border-radius: 8px; background: #fff; }}
    .header {{ text-align: center; margin-bottom: 20px; }}
    .btn {{ display: inline-block; padding: 10px 18px; border-radius: 6px; text-decoration: none; font-weight: bold; }}
    .primary {{ background:#0b74de; color:#fff; }}
    .note {{ font-size: 13px; color: #555; margin-top: 12px; }}
    .creds {{ background:#f7f7f7; padding:12px; border-radius:6px; margin:12px 0; }}
    .footer {{ font-size:12px; color:#777; text-align:center; margin-top:18px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h2>Welcome to {systemName}</h2>
      <p>Congratulations! The registration request for <strong>{ClincName}</strong> has been approved.</p>
    </div>

    <p>Here are your account details:</p>

    <div class='creds'>
      <p><strong>Username:</strong> {username}</p>
      <p><strong>Temporary Password:</strong> {password}</p>
    </div>

    <p>Click the button below to access the system:</p>
    <p style='text-align:center;'>
      <a href='{loginUrl}' class='btn primary'>Login to the System</a>
    </p>

    <p class='note'>
      <strong>Important Notes:</strong><br/>
      • Please change your password immediately after your first login from your account settings page.<br/>
      • The temporary password will expire on <strong>{passwordExpiryDate}</strong>.<br/>
      • For assistance, please contact us at <a href='mailto:{supportEmail}'>{supportEmail}</a> or call {supportPhone}.
    </p>

    <div class='footer'>
      Best regards,<br/>
      {systemName} Support Team
    </div>
  </div>
</body>
</html>";
        }
        private static string RejectClinicsRequestsMail(string ClincName)
        {
            string systemName = "Clinc Management System";
            string supportEmail = "support@your-system.com";
            string supportPhone = "+20 100 000 0000";

            return $@"
                        <!DOCTYPE html>
                        <html lang='en'>
                        <head>
                          <meta charset='utf-8' />
                          <meta name='viewport' content='width=device-width,initial-scale=1' />
                          <title>Clinc Registration Rejected - {ClincName}</title>
                          <style>
                            body {{ font-family: Arial, sans-serif; direction: ltr; color: #222; }}
                            .container {{ max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #eee; border-radius: 8px; background: #fff; }}
                            .header {{ text-align: center; margin-bottom: 20px; }}
                            .note {{ font-size: 14px; color: #555; margin-top: 12px; line-height: 1.6; }}
                            .footer {{ font-size:12px; color:#777; text-align:center; margin-top:18px; }}
                          </style>
                        </head>
                        <body>
                          <div class='container'>
                            <div class='header'>
                              <h2>{systemName}</h2>
                              <p>The registration request for <strong>{ClincName}</strong> has been reviewed.</p>
                              <h3 style='color:#d9534f;'>Registration Request Rejected</h3>
                            </div>
                        
                            <p class='note'>
                              We regret to inform you that your registration request has been declined.<br/><br/>
                              This decision may be due to missing information, verification issues, or not meeting the required registration criteria.
                            </p>
                        
                            <p class='note'>
                              If you believe this was a mistake or would like further clarification, please feel free to contact our support team at  
                              <a href='mailto:{supportEmail}'>{supportEmail}</a> or call us at {supportPhone}.
                            </p>
                        
                            <div class='footer'>
                              Best regards,<br/>
                              {systemName} Support Team
                            </div>
                          </div>
                        </body>
                        </html>";
        }

        private async Task<Pagination<ClincDTO>> GetClinicsAsync(Expression<Func<Clinic, bool>> CriteriaExpression, int pageIndex = 1, int pageSize = 10)
        {
            var ClinicRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(CriteriaExpression);
            spec.ApplyPaging((pageIndex - 1) * pageSize, pageSize);
            var totalItems = await ClinicRepo.CountAsync(spec);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            var list = await ClinicRepo.ListAsync(spec);
            var result = await Task.WhenAll(
                                          list.Select(async e => new ClincDTO
                                          {
                                              Id = e.Id,
                                              Name = e.Name,
                                              Email = e.Email,
                                              AddressCountry = e.AddressCountry,
                                              AddressGovernRate = e.AddressGovernRate,
                                              AddressCity = e.AddressCity,
                                              AddressLocation = e.AddressLocation,
                                              Phone = e.Phone,
                                              Status = e.Status,
                                              price = e.Price,
                                              LogoUrl = string.IsNullOrEmpty(e.LogoPath)
                                                   ? null
                                                   : await _uploadImageService.GetImageAsync(e.LogoPath),
                                              MedicalSpecialtyId = e.MedicalSpecialty?.Id,
                                              MedicalSpecialtyName = e.MedicalSpecialty?.Name
                                          }));
            var pagination = new Pagination<ClincDTO>(pageIndex, pageSize, totalItems, result);
            return pagination;
        }


        private async Task<bool> ChangeClinicStatusAsync(Expression<Func<Clinic, bool>> CriteriaExpression, ClinicStatus status)
        {

            try
            {
                var ClincRepo = _unitOfWork.Repository<Clinic>();
                var spec = new BaseSpecification<Clinic>(CriteriaExpression);
                var Clinc = await ClincRepo.GetEntityWithSpecAsync(spec);
                if (Clinc is null) throw new NotFoundException("this clinic not exsited");
                Clinc.Status = status.ToString();
                await ClincRepo.UpdateAsync(Clinc);
                if (await _unitOfWork.CompleteAsync() > 0) return true;

                throw new BadRequestException("faild to save clinic status");
            }
            catch (Exception ex)
            {
                if (ex is BadRequestException or UnauthorizedException or NotFoundException or ForbiddenException)
                    throw;
                // Log the exception
                throw new InternalServerException("An unexpected error occurred during Change Clinic Status. Please try again.");
            }
        }


        private static string GetClinicAdminAccountCreatedTemplate(
                string adminName,
                string clinicName,
                string adminEmail,
                string temporaryPassword,
                string activationLink,
                string supportEmail,
                string supportPhone,
                string organizationName,
                int year)
        {
            return $@"
<!doctype html>
<html lang='en'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Clinic Admin Account Created</title>
<style>
  body,table,td,a{{-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;}}
  table{{border-collapse:collapse!important;}}
  img{{border:0;height:auto;line-height:100%;outline:none;text-decoration:none;}}
  body{{margin:0;padding:0;width:100%!important;font-family:Arial,Helvetica,sans-serif;background-color:#f4f6f8;color:#333;}}
  .email-wrapper{{width:100%;padding:20px 0;}}
  .email-content{{max-width:680px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 4px 18px rgba(15,15,15,0.08);}}
  .email-header{{padding:20px 28px;display:flex;align-items:center;gap:16px;}}
  .brand-logo{{width:48px;height:48px;border-radius:6px;background:#e9eef6;text-align:center;line-height:48px;font-weight:bold;color:#1a73e8;}}
  .brand-title{{font-size:18px;font-weight:700;color:#1f2937;}}
  .email-body{{padding:24px 28px;font-size:15px;line-height:1.5;color:#374151;}}
  .greeting{{font-size:16px;font-weight:600;margin-bottom:12px;}}
  .lead{{margin-bottom:18px;color:#4b5563;}}
  .card{{background:#f8fafc;border:1px solid #e6eef7;padding:16px;border-radius:8px;margin-bottom:18px;}}
  .credential-row{{display:flex;gap:16px;flex-wrap:wrap;}}
  .cred-item{{min-width:160px;background:#fff;border:1px solid #e5e7eb;padding:10px 12px;border-radius:6px;font-family:monospace;font-size:14px;color:#111827;}}
  .btn{{display:inline-block;padding:12px 20px;background:#2563eb;color:#fff;text-decoration:none;border-radius:8px;font-weight:600;margin-top:8px;}}
  .muted{{color:#6b7280;font-size:13px;margin-top:12px;}}
  .email-footer{{padding:18px 28px;font-size:13px;color:#6b7280;border-top:1px solid #f1f5f9;}}
  @media (max-width:520px){{.email-header,.email-body,.email-footer{{padding-left:16px;padding-right:16px;}}.credential-row{{flex-direction:column;}}}}
</style>
</head>
<body>
  <center class='email-wrapper'>
    <div class='email-content'>
      <div class='email-header'>
        <div class='brand-logo'>CL</div>
        <div>
          <div class='brand-title'>Clinic Management System</div>
          <div style='font-size:13px;color:#6b7280;'>Account notification</div>
        </div>
      </div>

      <div class='email-body'>
        <div class='greeting'>Hello {adminName},</div>

        <div class='lead'>
          An administrator account for <strong>{clinicName}</strong> has been created in the Clinic Management System.
        </div>

        <div class='card'>
          <div style='font-size:14px;font-weight:600;margin-bottom:8px;'>Your account details</div>

          <div class='credential-row'>
            <div class='cred-item'>
              <div style='font-size:12px;color:#6b7280;'>Email</div>
              <div>{adminEmail}</div>
            </div>

            <div class='cred-item'>
              <div style='font-size:12px;color:#6b7280;'>Temporary password</div>
              <div>{temporaryPassword}</div>
            </div>

            <div class='cred-item' style='min-width:220px;'>
              <div style='font-size:12px;color:#6b7280;'>Role</div>
              <div>Clinic Administrator</div>
            </div>
          </div>

          <div style='margin-top:12px;font-size:13px;color:#374151;'>
            For security, please activate your account and change the temporary password.
          </div>

          <div style='margin-top:16px;'>
            <a class='btn' href='{activationLink}' target='_blank' rel='noopener'>Activate your account</a>
          </div>

          <div class='muted'>
            If the button doesn't work, copy & paste the following URL into your browser:
            <div style='word-break:break-all;'>{activationLink}</div>
          </div>
        </div>
        <div style='margin-top:18px;color:#374151;'>
          Thank you,<br>
          <strong>The Clinic Management Team</strong>
        </div>
      </div>

      <div class='email-footer'>
        Need help? Contact us at <a href='mailto:{supportEmail}'>{supportEmail}</a> or call {supportPhone}.<br>
        © {year} {organizationName}. All rights reserved.
      </div>
    </div>
  </center>
</body>
</html>";
        }

        private static string GetPasswordResetTemplate(
        string userName,
        string userEmail,
        string newPassword,
        string loginLink,
        string supportEmail,
        string supportPhone,
        string organizationName,
        int year)
        {
            return $@"
                    <!doctype html>
                    <html lang='en'>
                    <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width,initial-scale=1'>
                    <title>Password Reset Notification</title>
                    <style>
                      body,table,td,a{{-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;}}
                      table{{border-collapse:collapse!important;}}
                      img{{border:0;height:auto;line-height:100%;outline:none;text-decoration:none;}}
                      body{{margin:0;padding:0;width:100%!important;font-family:Arial,Helvetica,sans-serif;background-color:#f4f6f8;color:#333;}}
                      .email-wrapper{{width:100%;padding:20px 0;}}
                      .email-content{{max-width:680px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 4px 18px rgba(15,15,15,0.08);}}
                      .email-header{{padding:20px 28px;display:flex;align-items:center;gap:16px;}}
                      .brand-logo{{width:48px;height:48px;border-radius:6px;background:#e9eef6;text-align:center;line-height:48px;font-weight:bold;color:#1a73e8;}}
                      .brand-title{{font-size:18px;font-weight:700;color:#1f2937;}}
                      .email-body{{padding:24px 28px;font-size:15px;line-height:1.5;color:#374151;}}
                      .greeting{{font-size:16px;font-weight:600;margin-bottom:12px;}}
                      .lead{{margin-bottom:18px;color:#4b5563;}}
                      .card{{background:#f8fafc;border:1px solid #e6eef7;padding:16px;border-radius:8px;margin-bottom:18px;}}
                      .credential-row{{display:flex;gap:16px;flex-wrap:wrap;}}
                      .cred-item{{min-width:160px;background:#fff;border:1px solid #e5e7eb;padding:10px 12px;border-radius:6px;font-family:monospace;font-size:14px;color:#111827;}}
                      .btn{{display:inline-block;padding:12px 20px;background:#2563eb;color:#fff;text-decoration:none;border-radius:8px;font-weight:600;margin-top:8px;}}
                      .muted{{color:#6b7280;font-size:13px;margin-top:12px;}}
                      .email-footer{{padding:18px 28px;font-size:13px;color:#6b7280;border-top:1px solid #f1f5f9;}}
                      @media (max-width:520px){{.email-header,.email-body,.email-footer{{padding-left:16px;padding-right:16px;}}.credential-row{{flex-direction:column;}}}}
                    </style>
                    </head>
                    <body>
                      <center class='email-wrapper'>
                        <div class='email-content'>
                          <div class='email-header'>
                            <div class='brand-logo'>CL</div>
                            <div>
                              <div class='brand-title'>Clinic Management System</div>
                              <div style='font-size:13px;color:#6b7280;'>Security notification</div>
                            </div>
                          </div>
                    
                          <div class='email-body'>
                            <div class='greeting'>Hello {userName},</div>
                    
                            <div class='lead'>
                              Your password has been successfully reset. Below are your updated login credentials.
                            </div>
                    
                            <div class='card'>
                              <div style='font-size:14px;font-weight:600;margin-bottom:8px;'>New login details</div>
                    
                              <div class='credential-row'>
                                <div class='cred-item'>
                                  <div style='font-size:12px;color:#6b7280;'>Email</div>
                                  <div>{userEmail}</div>
                                </div>
                    
                                <div class='cred-item'>
                                  <div style='font-size:12px;color:#6b7280;'>New Password</div>
                                  <div>{newPassword}</div>
                                </div>
                              </div>
                    
                              <div style='margin-top:12px;font-size:13px;color:#374151;'>
                                For your security, please log in and change this temporary password immediately.
                              </div>
                    
                              <div style='margin-top:16px;'>
                                <a class='btn' href='{loginLink}' target='_blank' rel='noopener'>Login to your account</a>
                              </div>
                    
                              <div class='muted'>
                                If the button doesn't work, copy & paste this URL into your browser:
                                <div style='word-break:break-all;'>{loginLink}</div>
                              </div>
                            </div>
                    
                            <div style='margin-top:18px;color:#374151;'>
                              Stay safe,<br>
                              <strong>The Clinic Management Team</strong>
                            </div>
                          </div>
                    
                          <div class='email-footer'>
                            Need help? Contact us at <a href='mailto:{supportEmail}'>{supportEmail}</a> or call {supportPhone}.<br>
                            © {year} {organizationName}. All rights reserved.
                          </div>
                        </div>
                      </center>
                    </body>
                    </html>";
        }

        #endregion
    }
    public class ClincRegistrationDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string MedicalSpecialtyId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? AddressCountry { get; set; }
        public string? AddressGovernRate { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressLocation { get; set; }
        public string? Phone { get; set; }
        //public string Status { get; set; } = "pending";
    }
    public class ClincDTO
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MedicalSpecialtyId { get; set; } = string.Empty;
        public string? MedicalSpecialtyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AddressCountry { get; set; }
        public string? AddressGovernRate { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressLocation { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = ClinicStatus.pending.ToString();
        public double? price { get; set; }
        public string? LogoUrl { get; set; }
    }
    public static class ClincExtensions
    {
        public static Clinic ToClinc(this ClincRegistrationDTO Dto)
        {
            if (Dto is null)
            {
                return new Clinic();
            }

            return new Clinic
            {
                Name = Dto.Name,
                MedicalSpecialtyId = Dto.MedicalSpecialtyId,
                Email = Dto.Email,
                AddressCountry = Dto.AddressCountry,
                AddressGovernRate = Dto.AddressGovernRate,
                AddressCity = Dto.AddressCity,
                AddressLocation = Dto.AddressLocation,
                Phone = Dto.Phone,
                Status = ClinicStatus.pending.ToString(),
            };
        }
        public static ClincDTO ToClincDTO(this Clinic entity)
        {
            if (entity is null)
            {
                return new ClincDTO();
            }

            return new ClincDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                MedicalSpecialtyId = entity.MedicalSpecialtyId,
                MedicalSpecialtyName = entity.MedicalSpecialty?.Name ?? "NA",
                Email = entity.Email,
                AddressCountry = entity.AddressCountry,
                AddressGovernRate = entity.AddressGovernRate,
                AddressCity = entity.AddressCity,
                AddressLocation = entity.AddressLocation,
                Phone = entity.Phone,
                Status = entity.Status

            };
        }
        public static HashSet<ClincDTO> ToClincDTOSet(this IEnumerable<Clinic> entities)
        {
            if (entities == null)
                return new HashSet<ClincDTO>();

            return entities.Select(e => e.ToClincDTO()).ToHashSet();
        }
    }
    public class ClincAdminResetPasswordDTO
    {
        [Required]
        public required string AdminId { get; set; }
        [Required]
        public required string NewPassword { get; set; }
    }
    public class ClincAdminProfileCreateDTO
    {
        [Required]
        public required string ClincId { get; set; }
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string FullName { get; set; }
    }
    public class ClinicAdminDTO
    {
        public string userId { get; set; }
        public string FullName { get; set; }
    }
    enum ClinicStatus
    {
        pending,
        active,
        notactive
    }
    enum ClinicSearchType
    {
        Name,
        Email,
        Status
    }
}
