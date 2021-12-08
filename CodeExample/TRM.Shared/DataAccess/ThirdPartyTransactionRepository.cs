using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Linq;
using EPiServer.ServiceLocation;
using TRM.Shared.Models.DTOs;

namespace TRM.Shared.DataAccess
{
    public interface IThirdPartyTransactionRepository
    {
        bool AddOrUpdateTransaction(ThirdPartyTransaction transaction);
        ThirdPartyTransaction GetTransaction(string id);
        List<ThirdPartyTransaction> GetPendingTransactions();
        void BulkUpdateTransactions(List<ThirdPartyTransaction> transactions);
    }

    [ServiceConfiguration(typeof(IThirdPartyTransactionRepository), Lifecycle = ServiceInstanceScope.Transient)]
    public class ThirdPartyTransactionRepository : ThirdPartyTransactionRepositoryDbContext,
        IThirdPartyTransactionRepository
    {
        private readonly ThirdPartyTransactionRepositoryDbContext _context;

        public ThirdPartyTransactionRepository()
        {
            _context = new ThirdPartyTransactionRepositoryDbContext();
        }

        public bool AddOrUpdateTransaction(ThirdPartyTransaction transaction)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(transaction?.Id)) throw new Exception("Cannot insert the null record into the table ThirdPartyTransaction");

                transaction.LastChanged=DateTime.Now;

                _context.ThirdPartyTransactions.AddOrUpdate(transaction);
                _context.SaveChanges();
                return true;
            }
            catch 
            {
                return false;
            }
        }

        public ThirdPartyTransaction GetTransaction(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id)) return null;
                return _context.ThirdPartyTransactions.FirstOrDefault(x => x.Id == id);
            }
            catch
            {
                return null;
            }
        }

        public List<ThirdPartyTransaction> GetPendingTransactions()
        {
            try
            {
                var maxDate = DateTime.Now.AddMinutes(-10);
                return _context.ThirdPartyTransactions.Where(x => x.TransactionStatus == ThirdPartyTransactionStatus.Pending && x.LastChanged < maxDate).ToList();
            }
            catch
            {
                return null;
            }
        }

        public void BulkUpdateTransactions(List<ThirdPartyTransaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                if (transaction.TransactionStatus == ThirdPartyTransactionStatus.Success)
                {
                    _context.ThirdPartyTransactions.Remove(transaction);
                }
                else
                {
                    transaction.LastChanged = DateTime.Now;
                    _context.ThirdPartyTransactions.AddOrUpdate(transaction);
                }
            }
            _context.SaveChanges();
        }
    }


    public class ThirdPartyTransactionRepositoryDbContext : DbContext
    {
        public ThirdPartyTransactionRepositoryDbContext() : this(Shared.Constants.StringConstants.BullionCustomDatabaseName)
        {
        }

        public ThirdPartyTransactionRepositoryDbContext(string connString) : base(connString)
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<ThirdPartyTransactionRepositoryDbContext>());
        }

        public DbSet<ThirdPartyTransaction> ThirdPartyTransactions { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ThirdPartyTransaction>().ToTable("custom_ThirdPartyTransactions");
            modelBuilder.Properties<DateTime>().Configure(c => c.HasColumnType("datetime2"));
        }

        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
            }
        }
    }
}