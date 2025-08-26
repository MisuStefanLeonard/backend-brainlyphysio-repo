using backend.Models.DTO.AnnounceDto;
using backend.Models.Utils;
using backend.Services.CloudFlareCdnServices;
using backend.UOW;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Services.AnnounceService;

public class AnnounceService : IAnnounceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AnnounceService> _logger;
    private readonly ICloudFlareCdnService _cloudFlareCdnService;

    public AnnounceService(IUnitOfWork unitOfWork, ILogger<AnnounceService> logger, ICloudFlareCdnService cloudFlareCdnService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cloudFlareCdnService = cloudFlareCdnService;
    }

    public async Task<IList<AnnounceCrud>> GetAllAnnounces()
    {
        _logger.LogInformation("Getting all announces...");
        
        var getAllAnnounces = await _unitOfWork.Repository<Announce>()
            .GetSimpleQueryable()
            .Select(announce => new AnnounceCrud
            {
                IdAnnounce = announce.IdAnnounce,
                CourseTitle = announce.CourseTitle,
                CourseImagePath = announce.CourseImagePath,
                EmcPoints = announce.EmcPoints,
                AboutCourse = announce.AboutCourse,
                Format = announce.Format,
                Trainers = announce.Trainers,
                ContactPhoneNumber = announce.ContactPhoneNumber,
                Location = announce.Location,
                Price = announce.Price,
                CourseDuration = announce.CourseDuration,
                ComingDate = announce.ComingDate,
                LeavingDate = announce.LeavingDate,
                ExpirationDate = announce.ExpirationDate,
            }).ToListAsync();

        foreach (var announce in getAllAnnounces)
        {
            announce.PresignedUrl = await _cloudFlareCdnService.GeneratePresignedUrl(announce.CourseImagePath);
        }

        return getAllAnnounces;
    }

    public async Task<int> CreateAnnounce(AnnounceCrud announce, IFormFile image)
    {
        IDbContextTransaction? createTransaction = null;
        try
        {
            createTransaction = await _unitOfWork.BeginTransactionAsync();
            var announceRepository = _unitOfWork.Repository<Announce>();
            var courseRepository = _unitOfWork.Repository<CourseSection>();

            var isAnnounceInDb = await announceRepository
                .FindQueryable(a => a.CourseTitle == announce.CourseTitle)
                .FirstOrDefaultAsync();

            if(isAnnounceInDb != null)
            {
                _logger.LogInformation("Announce already exists");
                return -1;
            }
            
            _logger.LogInformation("Creating new announce...");

            var newAnnounce = new Announce
            {
                CourseTitle = announce.CourseTitle,
                CourseImagePath = image.FileName,
                EmcPoints = announce.EmcPoints,
                AboutCourse = announce.AboutCourse,
                Format = announce.Format,
                Trainers = announce.Trainers,
                ContactPhoneNumber = announce.ContactPhoneNumber,
                Location = announce.Location,
                Price = announce.Price,
                CourseDuration = announce.CourseDuration,
                ComingDate = announce.ComingDate,
                LeavingDate = announce.LeavingDate,
                ExpirationDate = announce.ExpirationDate,
            };
            
            _logger.LogInformation("Adding image...");
            using var memoryStream = new MemoryStream();
            memoryStream.Position = 0;
            await image.CopyToAsync(memoryStream);
            var response = await _cloudFlareCdnService.AddOrUpdateToCdnBucket(memoryStream, image.FileName);

            if (response == 1)
            {
                _logger.LogInformation("Successfully uploaded image to Cloudflare CDN for announce");
            }
            else
            {
                _logger.LogError("Error uploading image to Cloudflare CDN");
            }
            
            await announceRepository.AddAsync(newAnnounce);
            await _unitOfWork.CommitAsync();
            
            var announceId = newAnnounce.IdAnnounce;
            _logger.LogInformation("New announce created successfully");
            
            _logger.LogInformation("Creating new course sections...");
            // You only need one list for the parent objects
            var listCourseSections = new List<CourseSection>();

            foreach (var courseSectionDto in announce.CourseSections!)
            {
                var newCourseSection = new CourseSection
                {
                    SectionTitle = courseSectionDto.SectionTitle,
                    CourseSectionOrder = courseSectionDto.CourseSectionOrder,
                    IdAnnounce = announceId,
                    SectionContents = new List<SectionContent>() 
                };

                foreach (var sectionContentDto in courseSectionDto.SectionContents!)
                {
                    var newSectionContent = new SectionContent
                    {
                        Content = sectionContentDto.Content,
                        Order = sectionContentDto.Order,
                    };
                    newCourseSection.SectionContents.Add(newSectionContent); 
                }
    
                listCourseSections.Add(newCourseSection);
            }
            
            _logger.LogInformation("New course sections and their section content created successfully");
            
            await courseRepository.AddRangeAsync(listCourseSections);
            await _unitOfWork.CommitTransactionAsync(createTransaction);

            return 1;


        }
        catch (Exception e)
        {
            if (createTransaction != null)
            {
                _logger.LogError("Error occured. Rolling back transaction");
                await _unitOfWork.RollBackTransactionAsync(createTransaction);
            }
            _logger.LogError(e, "Error creating announce");
            return 1;
        }
    }
    
    public async Task<int> ModifyAnnounce(AnnounceCrud announce, IFormFile? image)
    {
        IDbContextTransaction? updateTransaction = null;
        try
        {
            _logger.LogInformation($"Starting to modify announce with ID: {announce.IdAnnounce}...");
            updateTransaction = await _unitOfWork.BeginTransactionAsync();

            var announceRepository = _unitOfWork.Repository<Announce>();
            var sectionRepository = _unitOfWork.Repository<CourseSection>();
            var contentRepository = _unitOfWork.Repository<SectionContent>();

            // 1. Fetch the existing Announce from the DB with all its children
            var announceToUpdate = await announceRepository.GetSimpleQueryable()
                .Include(a => a.CourseSections)!
                .ThenInclude(cs => cs.SectionContents)
                .FirstOrDefaultAsync(a => a.IdAnnounce == announce.IdAnnounce);

            if (announceToUpdate is null)
            {
                _logger.LogError($"Announce with ID {announce.IdAnnounce} not found.");
                return -1;
            }

            // 2. Handle Image Update
            if (image is { Length: > 0 })
            {
                _logger.LogInformation("New image provided. Updating image on CDN...");
                var oldImagePath = announceToUpdate.CourseImagePath;
                var newImageKey = $"images/{image.FileName}"; // Always use the full key

                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                var response = await _cloudFlareCdnService.AddOrUpdateToCdnBucket(memoryStream, newImageKey);

                if (response == 1)
                {
                    if (!string.IsNullOrEmpty(oldImagePath))
                    {
                        await _cloudFlareCdnService.DeleteFromCdnBucket(oldImagePath);
                    }
                    announceToUpdate.CourseImagePath = newImageKey;
                    _logger.LogInformation("Successfully replaced image on CDN.");
                }
                else
                {
                    _logger.LogError("Error uploading new image to CDN. Aborting update.");
                    await _unitOfWork.RollBackTransactionAsync(updateTransaction);
                    return -1;
                }
            }

            // 3. Update main Announce properties
            announceToUpdate.CourseTitle = announce.CourseTitle;
            announceToUpdate.EmcPoints = announce.EmcPoints;
            announceToUpdate.AboutCourse = announce.AboutCourse;
            announceToUpdate.Format = announce.Format;
            announceToUpdate.Trainers = announce.Trainers;
            announceToUpdate.ContactPhoneNumber = announce.ContactPhoneNumber;
            announceToUpdate.Location = announce.Location;
            announceToUpdate.Price = announce.Price;
            announceToUpdate.CourseDuration = announce.CourseDuration;
            announceToUpdate.ComingDate = announce.ComingDate;
            announceToUpdate.LeavingDate = announce.LeavingDate;
            announceToUpdate.ExpirationDate = announce.ExpirationDate;

            // 4. Synchronize Course Sections and their Content
            var dtoSectionIds = announce.CourseSections!.Select(cs => cs.IdCourseSection).ToHashSet();

            // 4a. Delete sections that are in the DB but not in the DTO
            var sectionsToRemove = announceToUpdate.CourseSections!
                .Where(cs => cs.IdCourseSection != 0 && !dtoSectionIds.Contains(cs.IdCourseSection))
                .ToList();
            if (sectionsToRemove.Any())
            {
                _logger.LogInformation($"Removing {sectionsToRemove.Count} course sections...");
                await sectionRepository.DeleteRangeAsync(sectionsToRemove);
            }

            // 4b. Update existing sections and add new ones
            foreach (var sectionDto in announce.CourseSections!)
            {
                CourseSection? existingSection;
                if (sectionDto.IdCourseSection > 0) // It's an existing section
                {
                    existingSection = announceToUpdate.CourseSections!.FirstOrDefault(cs => cs.IdCourseSection == sectionDto.IdCourseSection);
                    if (existingSection != null)
                    {
                        // --- REORDERING LOGIC FOR SECTIONS ---
                        if (existingSection.CourseSectionOrder != sectionDto.CourseSectionOrder)
                        {
                            var sectionToSwapWith = announceToUpdate.CourseSections!.FirstOrDefault(s => s.CourseSectionOrder == sectionDto.CourseSectionOrder);
                            if (sectionToSwapWith != null)
                            {
                                sectionToSwapWith.CourseSectionOrder = existingSection.CourseSectionOrder; // Swap order
                            }
                        }
                        existingSection.SectionTitle = sectionDto.SectionTitle;
                        existingSection.CourseSectionOrder = sectionDto.CourseSectionOrder;
                    }
                }
                else // It's a new section
                {
                    existingSection = new CourseSection
                    {
                        SectionTitle = sectionDto.SectionTitle,
                        CourseSectionOrder = sectionDto.CourseSectionOrder,
                        SectionContents = new List<SectionContent>()
                    };
                    announceToUpdate.CourseSections!.Add(existingSection);
                }

                // --- Synchronize SectionContent for this section (new or existing) ---
                if (existingSection != null && sectionDto.SectionContents != null)
                {
                    var dtoContentIds = sectionDto.SectionContents.Select(sc => sc.IdSectionContent).ToHashSet();
                    var contentToRemove = existingSection.SectionContents!
                        .Where(sc => sc.IdSectionContent != 0 && !dtoContentIds.Contains(sc.IdSectionContent)).ToList();
                    
                    if (contentToRemove.Count != 0)
                    {
                        await contentRepository.DeleteRangeAsync(contentToRemove);
                    }

                    foreach (var contentDto in sectionDto.SectionContents)
                    {
                        if (contentDto.IdSectionContent > 0) // Existing content
                        {
                            var existingContent = existingSection.SectionContents!.FirstOrDefault(sc => sc.IdSectionContent == contentDto.IdSectionContent);
                            if (existingContent != null)
                            {
                               // --- REORDERING LOGIC FOR CONTENT ---
                               if (existingContent.Order != contentDto.Order)
                               {
                                   var contentToSwapWith = existingSection.SectionContents!.FirstOrDefault(c => c.Order == contentDto.Order);
                                   if (contentToSwapWith != null)
                                   {
                                       contentToSwapWith.Order = existingContent.Order; // Swap order
                                   }
                               }
                               existingContent.Content = contentDto.Content;
                               existingContent.Order = contentDto.Order;
                            }
                        }
                        else // New content
                        {
                            existingSection.SectionContents!.Add(new SectionContent
                            {
                                Content = contentDto.Content,
                                Order = contentDto.Order
                            });
                        }
                    }
                }
            }

            // 5. Save all changes and commit the transaction
            await _unitOfWork.CommitTransactionAsync(updateTransaction);
            
            _logger.LogInformation($"Successfully modified announce with ID: {announce.IdAnnounce}.");
            return 1;
        }
        catch (Exception e)
        {
            if (updateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateTransaction);
            }
            
            _logger.LogError(e, $"Error when trying to modify announce with ID: {announce.IdAnnounce}");
            return -1;
        }
    }

   public async Task<int> DeleteAnnounce(int idAnnounce)
    {
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            _logger.LogInformation($"Starting to delete announce with ID: {idAnnounce}...");
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            var announceRepository = _unitOfWork.Repository<Announce>();
    
            // 1. Find the announcement to delete
            var announceToDelete = await announceRepository.GetByIdAsync(idAnnounce);
    
            if (announceToDelete is null)
            {
                _logger.LogError($"Announce with ID {idAnnounce} not found for deletion.");
                return -1; // Or another status code for "Not Found"
            }
    
            // 2. Delete the associated image from the CDN
            _logger.LogInformation($"Deleting image '{announceToDelete.CourseImagePath}' from CDN...");
            var response = await _cloudFlareCdnService.DeleteFromCdnBucket(announceToDelete.CourseImagePath);
    
            if (response != 1)
            {
                // You might decide to continue or abort if the image deletion fails.
                // For this example, we log a warning but proceed with DB deletion.
                _logger.LogWarning($"Could not delete image '{announceToDelete.CourseImagePath}' from CDN, but proceeding with database deletion.");
            }
            else
            {
                _logger.LogInformation("Image successfully deleted from CDN.");
            }
            
            // 3. Delete the announcement from the database
            // EF Core will handle the cascading delete for related CourseSections and SectionContents
            await announceRepository.DeleteAsync(announceToDelete);
            
            // 4. Commit the transaction
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            
            _logger.LogInformation($"Successfully deleted announce with ID: {idAnnounce}.");
            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                _logger.LogError("An error occurred. Rolling back transaction.");
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            
            _logger.LogError(e, $"Error when trying to delete announce with ID: {idAnnounce}");
            return -1;
        }
    }

    public async Task<AnnounceCrud?> GetAnnounceById(int idAnnounce)
    {
        _logger.LogInformation($"Getting announce by id {idAnnounce}");

        var getAnnounceById = await _unitOfWork.Repository<Announce>()
            .FindQueryable(announce => announce.IdAnnounce == idAnnounce)
            .Select(announce => new AnnounceCrud
            {
                IdAnnounce = announce.IdAnnounce,
                CourseTitle = announce.CourseTitle,
                CourseImagePath = announce.CourseImagePath,
                PresignedUrl = null,
                EmcPoints = announce.EmcPoints,
                AboutCourse = announce.AboutCourse,
                Format = announce.Format,
                Trainers = announce.Trainers,
                ContactPhoneNumber = announce.ContactPhoneNumber,
                Location = announce.Location,
                Price = announce.Price,
                CourseDuration = announce.CourseDuration,
                ComingDate = announce.ComingDate,
                LeavingDate = announce.LeavingDate,
                ExpirationDate = announce.ExpirationDate,
                CourseSections = announce.CourseSections!
                    .OrderBy(a => a.CourseSectionOrder)
                    .Select(course => new CourseSectionDto
                    {
                        IdCourseSection = course.IdCourseSection,
                        SectionTitle = course.SectionTitle,
                        CourseSectionOrder = course.CourseSectionOrder,
                        SectionContents = course.SectionContents!
                            .OrderBy(a => a.Order)
                            .Select(content => new SectionContentDto
                            {
                                IdSectionContent = content.IdSectionContent,
                                Content = content.Content,
                                Order = content.Order,
                            }).ToList(),
                    }).ToList(),
            })
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (getAnnounceById is null)
        {
            _logger.LogError($"Announce with id {idAnnounce} not found.");
            return null;
        }

        getAnnounceById.PresignedUrl = await _cloudFlareCdnService.GeneratePresignedUrl(getAnnounceById.CourseImagePath);;
        
        return getAnnounceById;
    }
}