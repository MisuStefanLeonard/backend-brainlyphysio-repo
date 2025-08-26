using backend.Models.DTO.LocationDto;
using backend.Models.User;
using backend.UOW;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Services.LocationService;

public class LocationService : ILocationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LocationService> _logger;

    public LocationService(IUnitOfWork uow, ILogger<LocationService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<LocationCrud>> GetAllLocations()
    {
        var getLocations = await _uow.Repository<Location>()
            .GetSimpleQueryable()
            .Select(loc => new LocationCrud
            {
                IdLocation = loc.IdLocation,
                City = loc.City,
                County = loc.County
            }).ToListAsync();

        return getLocations;
    }

    public async Task<CreateLocationResponse> CreateLocation(LocationCrud loc)
    {
        IDbContextTransaction? createTransaction = null;
        try
        {
            _logger.LogInformation("Starting creating location from admin interface...");
            createTransaction = await _uow.BeginTransactionAsync();
            var locationRepository = _uow.Repository<Location>();

            var locationAlreadyExists = await locationRepository
                .FindQueryable(locDb => locDb.City == loc.City.Trim().ToLower()
                                        && locDb.County! == loc.County!.ToLower())
                .FirstOrDefaultAsync() != null;

            if (locationAlreadyExists)
            {
                _logger.LogError("Location already exists.");
                return new CreateLocationResponse
                {
                    Id = 0,
                    Message = "Location already exists"
                };           
            }
            
            
            var newLocation = new Location
            {
                City = loc.City.Trim(),
                County = loc.County!.Trim(),
            };
            

            await locationRepository.AddAsync(newLocation);
            await _uow.CommitAsync();
            
            var id = newLocation.IdLocation;
            
            await _uow.CommitTransactionAsync(createTransaction);
            _logger.LogInformation("Successfully created location from admin interface");
            return new CreateLocationResponse
            {
                Id = id,
                Message = "Successfully created location from admin interface"
            };    
        }
        catch (Exception e)
        {
            if (createTransaction != null)
            {
                await _uow.RollBackTransactionAsync(createTransaction);
            }

            _logger.LogError(e, "Error creating account");
            return new CreateLocationResponse
            {
                Id = -1,
                Message = "Error creating account"
            };    
        }
    }

    public async Task<int> ModifyLocation(LocationCrud loc)
    {
        IDbContextTransaction? updateTransaction = null;
        try
        {
            _logger.LogInformation("Starting modifying location data from admin interface...");
            updateTransaction = await  _uow.BeginTransactionAsync();
            var locationsRepository = _uow.Repository<Location>();
            
            var findLocation = await locationsRepository.GetByIdAsync(loc.IdLocation);

            if (findLocation is null)
            {
                _logger.LogError($"Location with id {loc.IdLocation} not found");
                return -1;
            }
            
            // _mapper.Map(loc, findLocation);
            findLocation.City = loc.City.Trim();
            findLocation.County = loc.County!.Trim();

            await locationsRepository.UpdateAsync(findLocation);
            await _uow.CommitTransactionAsync(updateTransaction);
            _logger.LogInformation("Successfully modified location from admin interface");
            return 1;
        }
        catch (Exception e)
        {
            if (updateTransaction != null)
            {
                await _uow.RollBackTransactionAsync(updateTransaction);
            }

            _logger.LogError(e, "Error when trying to modify location");
            return -1;
        }
    }

    public async Task<int> DeleteLocation(int idLocation)
    {
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            _logger.LogInformation("Starting deleting location data from admin interface...");
            deleteTransaction = await  _uow.BeginTransactionAsync();
            var locationsRepository = _uow.Repository<Location>();
            
            var findLocationToDelete = await locationsRepository.GetByIdAsync(idLocation);
            
            if (findLocationToDelete is null)
            {
                _logger.LogError($"Location with id {idLocation} not found");
                return -1;
            }
            
            await locationsRepository.DeleteAsync(findLocationToDelete);
            await _uow.CommitTransactionAsync(deleteTransaction);
            _logger.LogInformation("Successfully deleted location from admin interface");
            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                await _uow.RollBackTransactionAsync(deleteTransaction);
            }
            
            _logger.LogError(e, "Error when trying to delete location");
            return -1;
        }
    }
}