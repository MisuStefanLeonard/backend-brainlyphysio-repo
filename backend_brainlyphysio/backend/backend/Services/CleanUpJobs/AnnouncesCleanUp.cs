using backend.Models.Utils;
using backend.UOW;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Quartz;

namespace backend.Services.CleanUpJobs;

public class AnnouncesCleanUp : IJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AnnouncesCleanUp> _logger;

    public AnnouncesCleanUp(IUnitOfWork unitOfWork, ILogger<AnnouncesCleanUp> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            _logger.LogInformation("Announces job clean up executing.");

            var findAnnouncesThatExpired = await _unitOfWork.Repository<Announce>()
                .FindQueryable(announce => announce.ExpirationDate <= DateTime.Now)
                .ToListAsync();

            if (findAnnouncesThatExpired.Count == 0)
            {
                _logger.LogInformation("No announces found that expired to delete");
            }
            else
            {
                _logger.LogInformation("Deleting announces that expired...");
                await _unitOfWork.Repository<Announce>().DeleteRangeAsync(findAnnouncesThatExpired);
                await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            }
        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                _logger.LogError("Announces delete transaction failed");
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            _logger.LogError($"{e.Message}\n{e.StackTrace}");
        }
    }
}