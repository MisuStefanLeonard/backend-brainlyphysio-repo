using backend.Models.User;
using backend.Models.Utils;
using Microsoft.EntityFrameworkCore;

namespace backend.Context;

public class DbContextBrainlyPhysio : DbContext
{
    public DbContextBrainlyPhysio(DbContextOptions<DbContextBrainlyPhysio> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quality>(entity =>
        {
            entity.ToTable("calitati");
            entity.HasKey(e => e.IdQuality);

            entity.Property(e => e.IdQuality)
                .HasColumnName("id_calitate")
                .HasColumnType("int(1)");

            entity.Property(e => e.QualityName)
                .HasColumnName("nume_calitate")
                .HasColumnType("varchar(150)")
                .IsRequired();

            entity.HasMany(e => e.MemberQualities)
                .WithOne(e => e.Quality)
                .HasForeignKey(e => e.IdQuality)
                .IsRequired();
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("conturi");
            entity.HasKey(e => e.IdAccount);

            entity.Property(e => e.IdAccount)
                .HasColumnName("id_cont")
                .HasColumnType("int(1)");

            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("nume")
                .HasColumnType("varchar");

            entity.Property(e => e.Prename)
                .HasMaxLength(70)
                .HasColumnName("prenume")
                .HasColumnType("varchar");

            entity.Property(e => e.PhoneNumber)
                .HasColumnName("nr_telefon")
                .HasColumnType("varchar")
                .HasMaxLength(20);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasColumnType("varchar")
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("descriere")
                .HasColumnType("varchar")
                .HasMaxLength(150);

            entity.HasIndex(e => e.Email)
                .HasDatabaseName("INDEX_EMAIL")
                .IsUnique();

            entity.Property(e => e.Password)
                .HasColumnName("parola")
                .HasColumnType("varchar")
                .HasMaxLength(100);

            entity.Property(e => e.ImagePath)
                .HasColumnName("cale_imagine")
                .HasColumnType("varchar")
                .HasMaxLength(255);

            entity.Property(e => e.ImageHash)
                .HasColumnName("image_hash")
                .HasColumnType("varchar")
                .HasMaxLength(50);

            entity.Property(e => e.ActivationCode)
                .HasColumnName("cod_activare")
                .HasColumnType("varchar")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.IsVerified)
                .HasColumnName("verificat")
                .HasColumnType("tinyint")
                .IsRequired();

            entity.Property(e => e.Role)
                .HasColumnName("rol")
                .HasColumnType("varchar")
                .HasMaxLength(15)
                .IsRequired();

            entity.Property(e => e.ConfirmationLinkHour)
                .HasColumnName("ora_link")
                .HasColumnType("datetime")
                .HasDefaultValueSql("NOW()");

            entity.HasMany(e => e.Qualities)
                .WithOne(e => e.Member)
                .HasForeignKey(e => e.IdMember)
                .IsRequired();

            entity.HasMany(e => e.OperationPlaces)
                .WithOne(e => e.Account)
                .HasForeignKey(e => e.IdAccount)
                .IsRequired();

        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locatii");
            entity.HasKey(e => e.IdLocation);

            entity.Property(e => e.IdLocation)
                .HasColumnName("id_locatie")
                .HasColumnType("int(1)");


            entity.Property(e => e.City)
                .HasColumnName("oras")
                .HasColumnType("varchar")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.County)
                .HasColumnName("judet")
                .HasColumnType("varchar")
                .HasMaxLength(50);

            entity.HasMany(e => e.OperationPlaces)
                .WithOne(e => e.Location)
                .HasForeignKey(e => e.IdLocation)
                .IsRequired();
        });


        modelBuilder.Entity<MemberQuality>(entity =>
        {
            entity.ToTable("calitati_membru");
            entity.HasKey(e => e.IdMemberQuality);

            entity.Property(e => e.IdMemberQuality)
                .HasColumnName("id_calitate_membru")
                .HasColumnType("int(1)");

            entity.Property(e => e.IdQuality)
                .HasColumnName("id_calitate")
                .HasColumnType("int")
                .IsRequired();

            entity.Property(e => e.IdMember)
                .HasColumnName("id_membru")
                .HasColumnType("int")
                .IsRequired();
        });

        modelBuilder.Entity<OperationPlace>(entity =>
        {
            entity.ToTable("locuri_de_operare");
            entity.HasKey(e => e.IdOperationPlace);

            entity.Property(e => e.IdOperationPlace)
                .HasColumnName("id_loc_operare")
                .HasColumnType("int(1)");

            entity.Property(e => e.IdLocation)
                .HasColumnName("id_locatie")
                .HasColumnType("int")
                .IsRequired();

            entity.Property(e => e.IdAccount)
                .HasColumnName("id_membru")
                .HasColumnType("int")
                .IsRequired();
        });

        modelBuilder.Entity<RememberUser>(entity =>
        {
            entity.ToTable("sesiuni");
            entity.HasKey(e => e.IdSession);

            entity.Property(e => e.IdSession)
                .HasColumnName("id_sesiune")
                .HasColumnType("int(1)");


            entity.Property(e => e.IdAccount)
                .HasColumnName("id_cont")
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.SessionToken)
                .HasColumnType("varchar")
                .HasColumnName("sesiune_stocata")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.IssuedAt)
                .HasColumnType("datetime")
                .HasColumnName("issued_at")
                .HasDefaultValueSql("NOW()")
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at")
                .HasDefaultValueSql("(NOW() + INTERVAL 30 DAY)")
                .IsRequired();

            entity.HasOne(e => e.CurrentUserSession)
                .WithOne(c => c.RememberUserSession)
                .HasForeignKey<RememberUser>(e => e.IdAccount)
                .IsRequired();
        });

        modelBuilder.Entity<Announce>(entity =>
        {
            entity.ToTable("anunturi");
            entity.HasKey(e => e.IdAnnounce);
            ;

            entity.Property(e => e.IdAnnounce)
                .HasColumnName("id_anunt")
                .HasColumnType("int(1)");

            entity.Property(e => e.CourseTitle)
                .HasColumnName("titlu_curs")
                .HasColumnType("varchar")
                .HasMaxLength(70)
                .IsRequired();

            entity.Property(e => e.CourseImagePath)
                .HasColumnName("cale_imagine_curs")
                .HasColumnType("varchar")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.EmcPoints)
                .HasColumnName("puncte_emc")
                .HasColumnType("tinyint");

            entity.Property(e => e.AboutCourse)
                .HasColumnName("descriere_curs")
                .HasColumnType("varchar")
                .HasMaxLength(255);

            entity.Property(e => e.Format)
                .HasColumnName("format")
                .HasColumnType("varchar")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Trainers)
                .HasColumnName("traineri")
                .HasColumnType("varchar")
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(e => e.ContactPhoneNumber)
                .HasColumnName("nr_telefon_contact")
                .HasColumnType("varchar")
                .HasMaxLength(20);

            entity.Property(e => e.Location)
                .HasColumnName("locatie")
                .HasColumnType("varchar")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Price)
                .HasColumnName("pret")
                .HasColumnType("int");
            
            entity.Property(e => e.CourseDuration)
                .HasColumnName("durata_curs")
                .HasColumnType("int")
                .IsRequired();

            entity.Property(e => e.ComingDate)
                .HasColumnName("data_venire")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(e => e.LeavingDate)
                .HasColumnName("data_plecare")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(e => e.ExpirationDate)
                .HasColumnName("data_expirare")
                .HasColumnType("date")
                .IsRequired();
            
            entity.HasMany(e => e.CourseSections)
                .WithOne(e => e.SectionAnnounce)
                .HasForeignKey(e => e.IdAnnounce)
                .IsRequired();

        });
        
        modelBuilder.Entity<CourseSection>(entity =>
        {
            entity.ToTable("sectiuni_curs");
            entity.HasKey(e => e.IdCourseSection);

            entity.Property(e => e.IdCourseSection)
                .HasColumnName("id_sectiune_curs")
                .HasColumnType("int(1)");

            entity.Property(e => e.SectionTitle)
                .HasColumnName("titlu_sectiune")
                .HasColumnType("varchar")
                .HasMaxLength(150); // You can adjust the length as needed

            entity.Property(e => e.CourseSectionOrder)
                .HasColumnName("ordine_sectiune")
                .HasColumnType("int")
                .IsRequired();

            entity.Property(e => e.IdAnnounce)
                .HasColumnName("id_anunt")
                .HasColumnType("int")
                .IsRequired();

            entity.HasMany(e => e.SectionContents)
                .WithOne(e => e.CourseSectionContent)
                .HasForeignKey(e => e.IdCourseSection)
                .IsRequired(); 
        });
        
        
        modelBuilder.Entity<SectionContent>(entity =>
        {
            entity.ToTable("continut_sectiune");
            entity.HasKey(e => e.IdSectionContent);

            entity.Property(e => e.IdSectionContent)
                .HasColumnName("id_continut_sectiune")
                .HasColumnType("int(1)");

            entity.Property(e => e.Content)
                .HasColumnName("continut")
                .HasColumnType("text") 
                .IsRequired();

            entity.Property(e => e.Order)
                .HasColumnName("ordine")
                .HasColumnType("int")
                .IsRequired();

            entity.Property(e => e.IdCourseSection)
                .HasColumnName("id_sectiune_curs")
                .HasColumnType("int")
                .IsRequired();
        });
    }
}