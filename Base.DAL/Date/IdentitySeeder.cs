using Base.DAL.Contexts;
using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Base.DAL.Seeding
{
    public static class IdentitySeeder
    {
        public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            var roleNames = Enum.GetNames<UserTypes>();
            // ✅ تأكد من وجود كل Role
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // 🧑‍💼 بيانات الأدمن الافتراضي
            string adminEmail = "islam7lmy@gmail.com";
            string adminPassword = "Admin@123";

            // ✅ تحقق لو الأدمن مش موجود
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    FullName = "Islam helmy",
                    Type = UserTypes.SystemAdmin,
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    // 🟣 أضف الأدمن إلى دور "Admin"
                    await userManager.AddToRoleAsync(adminUser, UserTypes.SystemAdmin.ToString());
                }
            }
        }
        public static async Task SeedDataAsync(AppDbContext context)
        {
            if (!context.MedicalSpecialties.Any())
            {
                var specialties = SeedMedicalSpecialty();
                context.MedicalSpecialties.AddRange(specialties);
            }
            await context.SaveChangesAsync();
        }
        public static List<MedicalSpecialty> SeedMedicalSpecialty()
        {
            return new List<MedicalSpecialty>
        {
            new MedicalSpecialty
            {
                Name = "الباطنة العامة",
                Description = "تخصص يُعنى بالتشخيص والعلاج غير الجراحي للأمراض التي تصيب الأعضاء الداخلية للكبار."
            },
            new MedicalSpecialty
            {
                Name = "طب الأطفال وحديثي الولادة",
                Description = "الرعاية الصحية للرضع، الأطفال والمراهقين، والتعامل مع أمراض الطفولة المختلفة."
            },
            new MedicalSpecialty
            {
                Name = "النساء والتوليد",
                Description = "يركز على صحة المرأة الإنجابية، الحمل، الولادة، وما بعد الولادة."
            },
            new MedicalSpecialty
            {
                Name = "الجلدية والتناسلية",
                Description = "تخصص يشمل تشخيص وعلاج الأمراض التي تصيب الجلد والشعر والأظافر، بالإضافة للأمراض التناسلية."
            },
            new MedicalSpecialty
            {
                Name = "الأنف والأذن والحنجرة",
                Description = "التشخيص الجراحي والطبي لأمراض الرأس والرقبة والأذن والأنف والحنجرة."
            },
            new MedicalSpecialty
            {
                Name = "جراحة العظام",
                Description = "يهتم بالجهاز العضلي الهيكلي، بما في ذلك العظام والمفاصل والأربطة والأوتار."
            },
            new MedicalSpecialty
            {
                Name = "طب وجراحة العيون",
                Description = "يُعنى بصحة العينين والرؤية، ويشمل التشخيص والعلاج الطبي والجراحي."
            },
            new MedicalSpecialty
            {
                Name = "جراحة التجميل والحروق",
                Description = "يهدف إلى إعادة بناء الهياكل الجسدية المشوهة نتيجة الإصابات أو العيوب الخلقية."
            },
            new MedicalSpecialty
            {
                Name = "جراحة المخ والأعصاب",
                Description = "التشخيص والعلاج الجراحي لاضطرابات الجهاز العصبي المركزي والمحيطي."
            },
            new MedicalSpecialty
            {
                Name = "أمراض القلب والأوعية الدموية",
                Description = "تخصص يُعنى بتشخيص وعلاج أمراض القلب والأوعية الدموية."
            },
            new MedicalSpecialty
            {
                Name = "المسالك البولية",
                Description = "تخصص يغطي المسالك البولية للذكور والإناث والأعضاء التناسلية للذكور."
            },
            new MedicalSpecialty
            {
                Name = "الأسنان",
                Description = "الوقاية والتشخيص والعلاج لأمراض الفم والأسنان واللثة."
            }
        };
        }
        //public static List<UserType> SeedUserType()
        //{
        //    return new List<UserType>()
        //    {
        //    new UserType(){ Name = "SystemAdmin"},
        //    new UserType(){ Name = "ClincAdmin"},
        //    new UserType(){ Name = "ClincDoctor"},
        //    new UserType(){ Name = "ClincReceptionis"},
        //    new UserType(){ Name = "User"},
        //    new UserType(){ Name = "SystemUser"}
        //    };
        //}
    }
}