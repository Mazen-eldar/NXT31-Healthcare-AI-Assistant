using Hangfire.Dashboard;

namespace Base.API.Filters
{

    public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context) => true;
    }
}
