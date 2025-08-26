using backend.Models.DTO.AnnounceDto;

namespace backend.Services.AnnounceService;

public interface IAnnounceService 
{
    Task<IList<AnnounceCrud>> GetAllAnnounces();
    Task<int> CreateAnnounce(AnnounceCrud announce, IFormFile image);
    Task<int> ModifyAnnounce(AnnounceCrud announce, IFormFile? image);
    Task<int> DeleteAnnounce(int idAnnounce);
    Task<AnnounceCrud?> GetAnnounceById(int idAnnounce);
}