using Base.API.DTOs;
using Base.Services.Implementations;
using Base.Shared.DTOs;
using Base.Shared.Responses.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Threading.Tasks;

namespace Base.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    [Route("api/[controller]")]
    // 🔒 الإبقاء على الحماية للمسؤولين لمنع أي مستخدم من إنشاء أدوار عشوائية
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // ✅ استخدام DTO للتحقق من الصحة التلقائي
        [HttpPost("create")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            // 1. التحقق الوقائي: الـ [ApiController] يتعامل تلقائيًا مع ModelState.IsValid
            if (string.IsNullOrEmpty(roleName))
            {
                throw new BadRequestException("Role Name is Required");
              
            }
            // 3. التحقق من وجود الدورالدور
            var exists = await _roleManager.RoleExistsAsync(roleName);
            if (exists)
            {
                // إرجاع خطأ 409 Conflict بدلاً من 400، للإشارة إلى تضارب المورد
                return Conflict($"Role '{roleName}' already exists.");
            }

            // 4. إنشاء الدور
            var newRole = new IdentityRole(roleName);
            var result = await _roleManager.CreateAsync(newRole);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new BadRequestException(errors);
            }

            // 6. إرجاع 201 Created مع بيانات الدور الجديد
            return Ok(new ApiResponseDTO(201, $"Role '{roleName}' created successfully."));
        }

        // ✅ يجب أن تكون هذه الدالة محمية بـ [Authorize(Roles = "Admin")] على مستوى المتحكم (Controller)
        [HttpGet("list")]
        public IActionResult GetRoles()
        {
            // 1. التحويل إلى DTO/Projection لتجنب تسريب أي بيانات غير ضرورية من كائن IdentityRole
            var result = _roleManager.Roles
                .Select(r => new { r.Id, r.Name }) // اختيار حقول محددة فقط
                .ToList();

            // 2. التحقق الوقائي: إرجاع 404 إذا لم يتم العثور على أدوار
            if (!result.Any())
            {
               throw new NotFoundException("No roles are currently defined in the system.");
            }
            return Ok(new ApiResponseDTO(200, "All Roles", result));
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            // 1️⃣ Validate input
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new BadRequestException("Role name is required.");
            }

            // 2️⃣ Try to find the role by name
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                throw new NotFoundException($"Role '{roleName}' does not exist.");
            }

            // 3️⃣ Attempt to delete the role
            var result = await _roleManager.DeleteAsync(role);

            // 4️⃣ Handle result
            if (!result.Succeeded)
            {
                throw new BadRequestException ( result.Errors.Select(e => e.Description));
            }

            // 5️⃣ Return success
            return Ok(new ApiResponseDTO(200,$"Role '{roleName}' deleted successfully."));
        }

    }
}
