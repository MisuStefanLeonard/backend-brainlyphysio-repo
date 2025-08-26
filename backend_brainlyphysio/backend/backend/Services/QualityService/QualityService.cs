using backend.Models.DTO.QualityDto;
using backend.Models.User;
using backend.UOW;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Services.QualityService;

public class QualityService : IQualityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QualityService> _logger;

    public QualityService(IUnitOfWork unitOfWork, ILogger<QualityService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<QualityCrud>> GetAllQualities()
    {
        var getQualities = await _unitOfWork.Repository<Quality>()
            .GetSimpleQueryable()
            .Select(q => new QualityCrud
            {
                IdQuality = q.IdQuality,
                QualityName = q.QualityName
            }).ToListAsync();

        return getQualities;
    }

    public async Task<CreateQualityResponseDto> CreateQuality(QualityCrud quality)
    {
        IDbContextTransaction? createTransaction = null;
        try
        {
            _logger.LogInformation("Starting creating quality...");
            createTransaction = await _unitOfWork.BeginTransactionAsync();
            var qualityRepository = _unitOfWork.Repository<Quality>();
            
            var alreadyExists = await qualityRepository
                .FindQueryable(q => q.QualityName == quality.QualityName)
                .FirstOrDefaultAsync() != null;

            if (alreadyExists)
            {
                _logger.LogError("Quality already exists.");
                return new CreateQualityResponseDto
                {
                    Id = 0,
                    Message = "Quality already exists"
                };           
            }
            
            var newQuality = new Quality
            {
                QualityName = quality.QualityName.Trim(),
            };
            _logger.LogInformation($"IDQUALITY {newQuality.IdQuality}");
            

            await qualityRepository.AddAsync(newQuality);
            await _unitOfWork.CommitAsync();
            var id = newQuality.IdQuality;
            await _unitOfWork.CommitTransactionAsync(createTransaction);
            _logger.LogInformation("Successfully created quality.");
            return new CreateQualityResponseDto
            {
                Id = id,
                Message = "Successfully created quality"
            };   
        }
        catch (Exception e)
        {
            if (createTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(createTransaction);
            }

            _logger.LogError(e, "Error creating quality");
            return new CreateQualityResponseDto
            {
                Id = 0,
                Message = "Error creating quality"
            };   
        }
    }

    public async Task<int> ModifyQuality(QualityCrud quality)
    {
        IDbContextTransaction? updateTransaction = null;
        try
        {
            _logger.LogInformation("Starting modifying quality data...");
            updateTransaction = await  _unitOfWork.BeginTransactionAsync();
            var qualityRepository = _unitOfWork.Repository<Quality>();
            
            var findQuality = await qualityRepository.GetByIdAsync(quality.IdQuality);

            if (findQuality is null)
            {
                _logger.LogError($"Quality with id {quality.IdQuality} not found");
                return -1;
            }
            
            findQuality.QualityName = quality.QualityName;
            
            await qualityRepository.UpdateAsync(findQuality);
            await _unitOfWork.CommitTransactionAsync(updateTransaction);
            _logger.LogInformation("Successfully modified quality.");
            return 1;
        }
        catch (Exception e)
        {
            if (updateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateTransaction);
            }

            _logger.LogError(e, "Error when trying to modify quality");
            return -1;
        }
    }

    public async Task<int> DeleteQuality(int idQuality)
    {
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            _logger.LogInformation("Starting deleting quality data...");
            deleteTransaction = await  _unitOfWork.BeginTransactionAsync();
            var qualityRepository = _unitOfWork.Repository<Quality>();
            
            var findQualityToDelete = await qualityRepository.GetByIdAsync(idQuality);
            
            if (findQualityToDelete is null)
            {
                _logger.LogError($"Quality with id {idQuality} not found");
                return -1;
            }
            
            await qualityRepository.DeleteAsync(findQualityToDelete);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            _logger.LogInformation("Successfully deleted quality.");
            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            
            _logger.LogError(e, "Error when trying to delete quality");
            return -1;
        }
    }
}