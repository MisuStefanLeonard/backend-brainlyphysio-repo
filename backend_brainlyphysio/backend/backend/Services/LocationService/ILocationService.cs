using backend.Models.DTO.LocationDto;

namespace backend.Services.LocationService;

public interface ILocationService
{
    Task<List<LocationCrud>> GetAllLocations();
    Task<CreateLocationResponse> CreateLocation(LocationCrud loc);
    Task<int> ModifyLocation(LocationCrud loc);
    Task<int> DeleteLocation(int idLocation);
    
}