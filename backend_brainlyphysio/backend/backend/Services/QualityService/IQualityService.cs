using backend.Models.DTO.QualityDto;

namespace backend.Services.QualityService;

public interface IQualityService 
{
    Task<List<QualityCrud>> GetAllQualities();
    Task<CreateQualityResponseDto> CreateQuality(QualityCrud quality);
    Task<int> ModifyQuality(QualityCrud quality);
    Task<int> DeleteQuality(int idQuality);
}