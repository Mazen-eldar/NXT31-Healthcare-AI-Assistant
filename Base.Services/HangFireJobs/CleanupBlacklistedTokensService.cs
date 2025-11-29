using Base.Repo.Interfaces;
using Base.Shared.Responses;
using RepositoryProject.Specifications;

namespace Base.Services.HangFireJobs
{
    public class CleanupBlacklistedTokensService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CleanupBlacklistedTokensService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                var repo = _unitOfWork.Repository<BlacklistedToken>();
                var spec = new BaseSpecification<BlacklistedToken>(t => t.ExpiryDate <= DateTime.UtcNow);

                var expiredTokens = await repo.ListAsync(spec);

                if (expiredTokens != null && expiredTokens.Any())
                {
                    await repo.RemoveRangeAsync(expiredTokens);
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                // تسجيل أي خطأ
                Console.WriteLine($"Error cleaning blacklisted tokens: {ex.Message}");
            }
        }


    }

}
