using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Base.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = "SystemAdmin")]
    [Authorize(Policy = "ActiveUserOnly")]
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UsersController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        // GET: api/users
        [HttpGet("list")]
        public async Task<ActionResult<UserListDto>> GetAll(
            [FromQuery] string? search,
            [FromQuery] UserTypes? userType,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _userProfileService.GetAllAsync(search, userType, isActive, page, pageSize);
            return Ok(result);
        }

        // GET: api/users/{id}
        [HttpGet("get-user")]
        public async Task<ActionResult<UserDto>> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            var user = await _userProfileService.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // POST: api/users
        [HttpPost("create")]
        public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var user = await _userProfileService.CreateAsync(request);
            return Ok(user);
        }

        // PUT: api/users/{id}
        [HttpPut("update")]
        public async Task<ActionResult<UserDto>> Update(string id, UpdateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (request == null) throw new ArgumentNullException(nameof(request));
            var user = await _userProfileService.UpdateAsync(id, request);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // PATCH: api/users/{id}/toggle-active
        [HttpPatch("toggle-active")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            var success = await _userProfileService.ToggleActiveAsync(id);
            if (!success) return Forbid();
            return Ok();
        }

        // DELETE: api/users/{id}
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            var success = await _userProfileService.DeleteAsync(id);
            if (!success) return Forbid();
            return Ok();
        }

        // PATCH: api/users/{id}/change-password
        [HttpPatch("change-password")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] string newPassword)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(newPassword)) throw new ArgumentNullException(nameof(newPassword));
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return BadRequest("Password must be at least 6 characters.");

            var success = await _userProfileService.ChangePasswordAsync(id, newPassword);
            if (!success) return Forbid();
            return Ok();
        }
    }
}

