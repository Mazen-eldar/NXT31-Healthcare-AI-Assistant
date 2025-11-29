using Base.API.DTOs;
using Base.DAL.Models.BaseModels;
using Base.Services.Implementations;
using Base.Shared.DTOs;
using Base.Shared.Responses.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Base.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    [Route("api/[controller]")]
    // 🔒 الإبقاء على الحماية لدور المدير هو أهم خطوة وقائية في هذا المتحكم
    [Authorize(Roles = "Admin")]
    public class UserRolesController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUserToRole([FromBody] UserRoleDTO model)
        {
            // 1. التحقق الوقائي التلقائي عبر [ApiController]
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }

            // 2. التحقق الوقائي: البحث عن المستخدم
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                // نستخدم NotFound (404) بدلاً من BadRequest (400) للدلالة على عدم وجود المورد (المستخدم)
                throw new NotFoundException($"User with email '{model.Email}' not found.");
            }

            // 3. التحقق الوقائي: البحث عن الدور
            var roleExists = await _roleManager.RoleExistsAsync(model.Role.Trim());
            if (!roleExists)
            {
                throw new NotFoundException($"Role '{model.Role}' does not exist.");
            }

            // 4. التحقق الوقائي: هل المستخدم بالفعل في الدور؟
            if (await _userManager.IsInRoleAsync(user, model.Role.Trim()))
            {
                // إرجاع 200 OK للإشارة إلى أن الطلب تحقق (Idempotent)
                return Ok(new ApiResponseDTO(201, $"User '{model.Email}' is already in role '{model.Role}'."));

            }


            // 5. إضافة الدور
            var result = await _userManager.AddToRoleAsync(user, model.Role.Trim());

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new BadRequestException(errors);
            }
            return Ok(new ApiResponseDTO(200, $"User '{model.Email}' added to role '{model.Role}' successfully."));

        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveUserFromRole([FromBody] UserRoleDTO model)
        {
            // 1. التحقق الوقائي التلقائي
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                throw new BadRequestException(errors);
            }

            // 2. التحقق الوقائي: البحث عن المستخدم
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                // 404 لعدم وجود المستخدم
                throw new NotFoundException($"User with email '{model.Email}' not found.");
            }

            // 3. التحقق الوقائي: البحث عن الدور
            var roleExists = await _roleManager.RoleExistsAsync(model.Role.Trim());
            if (!roleExists)
            {
                throw new NotFoundException($"Role '{model.Role}' does not exist.");
            }

            // 4. التحقق الوقائي: هل المستخدم في الدور بالفعل؟
            if (!await _userManager.IsInRoleAsync(user, model.Role.Trim()))
            {
                // إرجاع 200 OK للإشارة إلى أن المستخدم ليس في الدور الآن (Idempotent)
                throw new NotFoundException($"User '{model.Email}' is not currently in role '{model.Role}'.");
            }


            // 5. حذف الدور
            var result = await _userManager.RemoveFromRoleAsync(user, model.Role.Trim());

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new BadRequestException(errors);
            }
            return Ok(new ApiResponseDTO(200, $"User '{model.Email}' removed from role '{model.Role}' successfully."));
        }
      
        [HttpGet("getUserRoles")]
        public async Task<IActionResult> GetUserRoles(string email)
        {
            // 1. التحقق الوقائي: التحقق من أن الإيميل غير فارغ
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new BadRequestException("Email parameter is required.");
            }

            // 2. التحقق الوقائي: البحث عن المستخدم
            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                // 404 لعدم وجود المستخدم
                throw new NotFoundException($"User with email '{email}' not found.");
            }

            // 3. جلب الأدوار
            var roles = await _userManager.GetRolesAsync(user);

            // 4. إرجاع النتيجة
            return Ok(new ApiResponseDTO(200, $"List of Roles to User '{user.Id}'", roles.ToList()));
        }
    }
}
