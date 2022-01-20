using Microsoft.EntityFrameworkCore;

namespace ConnectionAlreadyOpenBug
{
    internal class TestDbContext : DbContext
    {
        public const string ConnectionString = "Server=127.0.0.1;User Id=postgres;Password=postgres;Database=TestDb_2;";

        public TestDbContext() : base()
        {
        }

        public TestDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString);
            optionsBuilder.LogTo(Console.WriteLine);
        }

        public DbSet<Locale> Locales { get; set; } = null!;

    }
}
