using Microsoft.EntityFrameworkCore;

namespace OpenMU.PlayerWeb.Data;

/// <summary>
/// EF Core context for the OpenMU PostgreSQL database.
/// Maps existing tables across the <c>config</c>, <c>data</c> and <c>guild</c> schemas.
/// The server owns these tables; this context only reads/writes specific columns the web needs.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterClass> CharacterClasses => Set<CharacterClass>();
    public DbSet<GameMapDefinition> GameMapDefinitions => Set<GameMapDefinition>();
    public DbSet<ItemStorage> ItemStorages => Set<ItemStorage>();
    public DbSet<StatAttribute> StatAttributes => Set<StatAttribute>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildMember> GuildMembers => Set<GuildMember>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();
    public DbSet<WheelSpin> WheelSpins => Set<WheelSpin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Character -> Account
        modelBuilder.Entity<Character>()
            .HasOne(c => c.Account)
            .WithMany(a => a.Characters)
            .HasForeignKey(c => c.AccountId);

        // Character -> CharacterClass
        modelBuilder.Entity<Character>()
            .HasOne(c => c.CharacterClass)
            .WithMany()
            .HasForeignKey(c => c.CharacterClassId);

        // Character -> GameMapDefinition
        modelBuilder.Entity<Character>()
            .HasOne(c => c.GameMapDefinition)
            .WithMany()
            .HasForeignKey(c => c.CurrentMapId);

        // Character -> ItemStorage (inventory)
        modelBuilder.Entity<Character>()
            .HasOne(c => c.Inventory)
            .WithMany()
            .HasForeignKey(c => c.InventoryId);

        // GuildMember -> Guild
        modelBuilder.Entity<GuildMember>()
            .HasOne(m => m.Guild)
            .WithMany()
            .HasForeignKey(m => m.GuildId);

        // GuildMember -> Character (shares PK with Character)
        modelBuilder.Entity<GuildMember>()
            .HasOne(m => m.Character)
            .WithMany()
            .HasForeignKey(m => m.Id);

        // StatAttribute -> Character
        modelBuilder.Entity<StatAttribute>()
            .HasOne(s => s.Character)
            .WithMany()
            .HasForeignKey(s => s.CharacterId);
    }
}
