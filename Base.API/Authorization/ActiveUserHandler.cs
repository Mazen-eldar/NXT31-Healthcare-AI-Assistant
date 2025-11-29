using Base.API.Controllers;
using Base.DAL.Models.BaseModels;
using Base.Repo.Interfaces;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
namespace Base.API.Authorization
{
    public class ActiveUserHandler : AuthorizationHandler<ActiveUserRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClinicServices _clinicServices;


        public ActiveUserHandler(UserManager<ApplicationUser> userManager, IClinicServices clinicServices)
        {
            _userManager = userManager;
            _clinicServices = clinicServices;
        }
        protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    ActiveUserRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                context.Fail();
                return;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                context.Fail();
                return;
            }

            // 1️⃣ لو اليوزر غير نشط
            if (!user.IsActive)
            {
                // نضع علامة في HttpContext
                var httpContext = context.Resource as DefaultHttpContext;
                httpContext?.Items.Add("UserIsInactive", true);

                context.Fail();
                return;
            }
            if (user.Type == UserTypes.SystemAdmin)
            {
                context.Succeed(requirement);
                return;
            }


            var clinicId = user.Type switch
            {
                UserTypes.ClinicDoctor => user.ClincDoctorProfile?.ClincId,
                UserTypes.ClinicReceptionist => user.ClincReceptionistProfile?.ClincId,
                UserTypes.ClinicAdmin => user.ClincAdminProfile?.ClincId,
                _ => null
            };

            // 2️⃣ تحقق من نوع المستخدم و الـ Clinic
            //if (Enum.TryParse<UserTypes>(user.Type, true, out var searchTypeEnum))
            //{
            //    var clinicId = searchTypeEnum switch
            //    {
            //        UserTypes.ClinicDoctor => user.ClincDoctorProfile?.ClincId,
            //        UserTypes.ClinicReceptionist => user.ClincReceptionistProfile?.ClincId,
            //        UserTypes.ClinicAdmin => user.ClincAdminProfile?.ClincId,
            //        _ => null
            //    };

            if (string.IsNullOrEmpty(clinicId))
            {
                context.Fail();
                return;
            }

            var clinic = await _clinicServices.GetClinicAsync(c => c.Id == clinicId && c.Status == ClinicStatus.active.ToString(), true);
            if (clinic == null)
            {
                // نضع علامة في HttpContext على سبب عدم النشاط
                var httpContext = context.Resource as DefaultHttpContext;
                httpContext?.Items.Add("UserIsInactive", true);

                context.Fail();
                return;
            }
            //}

            // لو كل شيء تمام
            context.Succeed(requirement);

        }
    }
}

