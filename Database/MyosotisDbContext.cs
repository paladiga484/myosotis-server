using Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Database;

public sealed class MyosotisDbContext(DbContextOptions<MyosotisDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPersonality> UserPersonalities => Set<UserPersonality>();
    public DbSet<UserEgo> UserEgos => Set<UserEgo>();
    public DbSet<UserItem> UserItems => Set<UserItem>();
    public DbSet<Formation> Formations => Set<Formation>();
    public DbSet<FormationName> FormationNames => Set<FormationName>();
    public DbSet<FormationDetail> FormationDetails => Set<FormationDetail>();
    public DbSet<FormationEgo> FormationEgos => Set<FormationEgo>();
    public DbSet<UserAnnouncer> UserAnnouncers => Set<UserAnnouncer>();
    public DbSet<AnnouncerPreset> AnnouncerPresets => Set<AnnouncerPreset>();
    public DbSet<AnnouncerPresetEntry> AnnouncerPresetEntries => Set<AnnouncerPresetEntry>();
    public DbSet<UserAnnouncerState> UserAnnouncerStates => Set<UserAnnouncerState>();
    public DbSet<UserBanner> UserBanners => Set<UserBanner>();
    public DbSet<ProfileTicket> ProfileTickets => Set<ProfileTicket>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ProfileBanner> ProfileBanners => Set<ProfileBanner>();
    public DbSet<ProfileSupportPersonality> ProfileSupportPersonalities => Set<ProfileSupportPersonality>();
    public DbSet<ProfileSupportEgo> ProfileSupportEgos => Set<ProfileSupportEgo>();
    public DbSet<RailwaySave> RailwaySaves => Set<RailwaySave>();
    public DbSet<RailwaySaveUnit> RailwaySaveUnits => Set<RailwaySaveUnit>();
    public DbSet<RailwaySaveUnitEgo> RailwaySaveUnitEgos => Set<RailwaySaveUnitEgo>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Uid);
            e.Property(x => x.Uid).ValueGeneratedOnAdd();
            e.HasIndex(x => x.Credential).IsUnique();
        });

        modelBuilder.Entity<UserPersonality>(e =>
        {
            e.ToTable("user_personalities");
            e.HasKey(x => new { x.Uid, x.PersonalityId });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserEgo>(e =>
        {
            e.ToTable("user_egos");
            e.HasKey(x => new { x.Uid, x.EgoId });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserItem>(e =>
        {
            e.ToTable("user_items");
            e.HasKey(x => new { x.Uid, x.ItemId });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Formation>(e =>
        {
            e.ToTable("formations");
            e.HasKey(x => x.FormationId);
            e.Property(x => x.FormationId).ValueGeneratedOnAdd();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Uid, x.Id }).IsUnique();
        });

        modelBuilder.Entity<FormationName>(e =>
        {
            e.ToTable("formation_names");
            e.HasKey(x => new { x.FormationId, x.K });
            e.HasOne<Formation>().WithMany().HasForeignKey(x => x.FormationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FormationDetail>(e =>
        {
            e.ToTable("formation_details");
            e.HasKey(x => new { x.FormationId, x.PersonalityId });
            e.HasOne<Formation>().WithMany().HasForeignKey(x => x.FormationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FormationEgo>(e =>
        {
            e.ToTable("formation_egos");
            e.HasKey(x => new { x.FormationId, x.PersonalityId, x.Idx });
            e.HasOne<FormationDetail>()
                .WithMany()
                .HasForeignKey(x => new { x.FormationId, x.PersonalityId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAnnouncer>(e =>
        {
            e.ToTable("user_announcers");
            e.HasKey(x => new { x.Uid, x.AnnouncerId });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnnouncerPreset>(e =>
        {
            e.ToTable("announcer_presets");
            e.HasKey(x => new { x.Uid, x.PresetId });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnnouncerPresetEntry>(e =>
        {
            e.ToTable("announcer_preset_entries");
            e.HasKey(x => new { x.Uid, x.PresetId, x.Idx });
            e.HasOne<AnnouncerPreset>()
                .WithMany()
                .HasForeignKey(x => new { x.Uid, x.PresetId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAnnouncerState>(e =>
        {
            e.ToTable("user_announcer_state");
            e.HasKey(x => x.Uid);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserBanner>(e =>
        {
            e.ToTable("user_banners");
            e.HasKey(x => new { x.Uid, x.Id });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileTicket>(e =>
        {
            e.ToTable("profile_tickets", t => t.HasCheckConstraint(
                "CK_profile_tickets_ticket_type", "ticket_type IN ('LEFT','RIGHT','EGOBG')"));
            e.HasKey(x => new { x.Uid, x.TicketType, x.Id });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(e =>
        {
            e.ToTable("user_profiles");
            e.HasKey(x => x.Uid);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileBanner>(e =>
        {
            e.ToTable("profile_banners");
            e.HasKey(x => new { x.Uid, x.Id });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileSupportPersonality>(e =>
        {
            e.ToTable("profile_support_personalities");
            e.HasKey(x => new { x.Uid, x.Idx });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileSupportEgo>(e =>
        {
            e.ToTable("profile_support_egos");
            e.HasKey(x => new { x.Uid, x.Idx, x.EgoIdx });
            e.HasOne<ProfileSupportPersonality>()
                .WithMany()
                .HasForeignKey(x => new { x.Uid, x.Idx })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RailwaySave>(e =>
        {
            e.ToTable("railway_saves");
            e.HasKey(x => new { x.Uid, x.DungeonId });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RailwaySaveUnit>(e =>
        {
            e.ToTable("railway_save_units");
            e.HasKey(x => new { x.Uid, x.DungeonId, x.Pord });
            e.HasOne<RailwaySave>()
                .WithMany()
                .HasForeignKey(x => new { x.Uid, x.DungeonId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RailwaySaveUnitEgo>(e =>
        {
            e.ToTable("railway_save_unit_egos");
            e.HasKey(x => new { x.Uid, x.DungeonId, x.Pord, x.Idx });
            e.HasOne<RailwaySaveUnit>()
                .WithMany()
                .HasForeignKey(x => new { x.Uid, x.DungeonId, x.Pord })
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
