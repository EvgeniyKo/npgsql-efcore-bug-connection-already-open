using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace ConnectionAlreadyOpenBug
{
    internal static class ShareConnection
    {
        public static async Task RunMicrosoftExample(bool rollback = true)
        {
            using (var connection = new NpgsqlConnection(TestDbContext.ConnectionString))
            {
                var builder = new DbContextOptionsBuilder<TestDbContext>()
                    .UseNpgsql(connection);
                var options = builder.Options;

                using var dbContext1 = new TestDbContext(options);
                using var transaction1 = dbContext1.Database.BeginTransaction();

                dbContext1.Add(new Locale { Code = "db1_a" });
                dbContext1.Add(new Locale { Code = "db1_b" });
                await dbContext1.SaveChangesAsync();

                using var dbContext2 = new TestDbContext(options);
                var transaction2 = dbContext2.Database.UseTransaction(transaction1.GetDbTransaction());

                dbContext2.Add(new Locale { Code = "db2_a" });
                dbContext2.Add(new Locale { Code = "db2_b" });
                await dbContext2.SaveChangesAsync();

                using var dbContext3 = new TestDbContext(options);
                var locales = await dbContext3.Locales.ToListAsync();
                Console.WriteLine("Locales: " + string.Join(", ", locales.Select(l => l.Code)));

                if (rollback)
                    transaction1.Rollback();
                else
                    transaction1.Commit();

                using var dbContext4 = new TestDbContext(options);
                locales = await dbContext3.Locales.ToListAsync();
                Console.WriteLine("Locales: " + string.Join(", ", locales.Select(l => l.Code)));
            }
        }

        public static async Task RunSharedTransaction(bool rollback = true)
        {
            using (var connection = new NpgsqlConnection(TestDbContext.ConnectionString))
            {
                connection.Open(); // need to open a connection before starting a transaction
                var transaction = connection.BeginTransaction();
                try
                {
                    var builder = new DbContextOptionsBuilder<TestDbContext>()
                        .UseNpgsql(connection);
                    var options = builder.Options;

                    using (var dbContext = new TestDbContext(options))
                    {
                        //await dbContext.Database.UseTransactionAsync(transaction); //  System.InvalidOperationException: 'Connection already open'
                        dbContext.Add(new Locale { Code = "db5_a" }); //  System.InvalidOperationException: 'Connection already open'
                        await dbContext.SaveChangesAsync();
                    }

                    using (var dbContext = new TestDbContext(options))
                    {
                        dbContext.Add(new Locale { Code = "db5_b" });
                        await dbContext.SaveChangesAsync();
                    }

                    if (rollback)
                        transaction.Rollback();
                    else
                        transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                }
            }
        }
    }
}
