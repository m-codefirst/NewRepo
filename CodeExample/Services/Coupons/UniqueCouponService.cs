using EPiServer.Framework.Cache;
using EPiServer.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace TRM.Web.Services.Coupons
{
    public class UniqueCoupon
    {
        public long Id { get; set; }
        public int PromotionId { get; set; }
        public string PromotionCode { get; set; }
        public string Code { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? Expiration { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime Created { get; set; }
        public int MaxRedemptions { get; set; }
        public int UsedRedemptions { get; set; }

        public bool IsRedeemed()
        {
            return UsedRedemptions >= MaxRedemptions;
        }
    }

    public interface ICouponService
    {
        bool SaveCoupons(List<UniqueCoupon> coupons);

        bool DeleteById(long id);

        bool DeleteByPromotionId(int id);

        bool DeleteExpiredCoupons();

        bool IsCodeReserved(string code);

        List<UniqueCoupon> GetByPromotionId(int id);

        UniqueCoupon GetById(long id);

        UniqueCoupon GetByCouponCode(string couponCode);

        void RedeemCoupon(string code);

        string GenerateCoupon();
        UniqueCoupon AssignCouponToCustomer(int id, Guid customerid);
    }

    public class UniqueCouponService : ICouponService
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(UniqueCouponService));
        private readonly CouponCodeBuilder _couponCodeBuilder = new CouponCodeBuilder();
        private readonly ISynchronizedObjectInstanceCache _cache;
        private const string CouponCachePrefix = "Extensions:UniqueCoupon:";
        private const string PromotionCachePrefix = "Extensions:Promotion:";

        private const string IdColumn = "Id";
        private const string PromotionIdColumn = "PromotionId";
        private const string PromotionCodeColumn = "PromotionCode";
        private const string CodeColumn = "Code";
        private const string ValidColumn = "Valid";

        private const string ExpirationColumn = "Expiration";
        private const string CustomerIdColumn = "CustomerId";
        private const string CreatedColumn = "Created";
        private const string MaxRedemptionsColumn = "MaxRedemptions";
        private const string UsedRedemptionsColumn = "UsedRedemptions";

        private const string ReturnCountName = "Count";

        public UniqueCouponService(ISynchronizedObjectInstanceCache cache)
        {
            _cache = cache;
        }

        public bool SaveCoupons(List<UniqueCoupon> coupons)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var command = new SqlCommand
                        {
                            Connection = transaction.Connection,
                            Transaction = transaction,
                            CommandType = CommandType.StoredProcedure,
                            CommandText = "UniqueCoupons_Save",
                            CommandTimeout = 90
                        };
                        command.Parameters.Add(new SqlParameter("@Data", CreateUniqueCouponsDataTable(coupons)));
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }

                foreach (var coupon in coupons)
                {
                    InvalidateCouponCache(coupon.Id);
                }

                return true;
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return false;
        }

        public bool DeleteById(long id)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var command = new SqlCommand
                        {
                            Connection = transaction.Connection,
                            Transaction = transaction,
                            CommandType = CommandType.StoredProcedure,
                            CommandText = "UniqueCoupons_DeleteById"
                        };
                        command.Parameters.Add(new SqlParameter("@Id", id));
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    InvalidateCouponCache(id);
                }

                return true;
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return false;
        }

        public bool DeleteByPromotionId(int id)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var command = new SqlCommand
                        {
                            Connection = transaction.Connection,
                            Transaction = transaction,
                            CommandType = CommandType.StoredProcedure,
                            CommandText = "UniqueCoupons_DeleteByPromotionId"
                        };
                        command.Parameters.Add(new SqlParameter("@PromotionId", id));
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    InvalidatePromotionCache(id);
                }

                return true;
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return false;
        }

        public bool DeleteExpiredCoupons()
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var command = new SqlCommand
                        {
                            Connection = transaction.Connection,
                            Transaction = transaction,
                            CommandType = CommandType.StoredProcedure,
                            CommandText = "UniqueCoupons_DeleteExpiredCoupons"
                        };

                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }

                return true;
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return false;
        }

        public bool IsCodeReserved(string code)
        {
            var count = 0;

            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand
                    {
                        Connection = connection,
                        CommandType = CommandType.Text,
                        CommandText = $"SELECT COUNT(*) as {ReturnCountName} FROM [UniqueCoupons] WHERE PromotionCode = @Code"
                    };
                    command.Parameters.Add(new SqlParameter("@Code", code));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            count = GetCount(reader);
                        }
                    }
                }
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return count > 0;
        }

        public List<UniqueCoupon> GetByPromotionId(int id)
        {
            try
            {
                return _cache.ReadThrough(GetPromotionCacheKey(id), () =>
                {
                    var coupons = new List<UniqueCoupon>();
                    var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        var command = new SqlCommand
                        {
                            Connection = connection,
                            CommandType = CommandType.StoredProcedure,
                            CommandText = "UniqueCoupons_GetByPromotionId"
                        };
                        command.Parameters.Add(new SqlParameter("@PromotionId", id));
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                coupons.Add(GetUniqueCoupon(reader));
                            }
                        }
                    }

                    return coupons.Count == 0 ? null : coupons;
                }, x => GetCacheEvictionPolicy(x), ReadStrategy.Wait);
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return null;
        }

        public UniqueCoupon GetById(long id)
        {
            try
            {
                return _cache.ReadThrough(GetCouponCacheKey(id), () =>
                {
                    UniqueCoupon coupon = null;
                    var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        var command = new SqlCommand
                        {
                            Connection = connection,
                            CommandType = CommandType.StoredProcedure,
                            CommandText = "UniqueCoupons_GetById"
                        };
                        command.Parameters.Add(new SqlParameter("@Id", id));
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                coupon = GetUniqueCoupon(reader);
                            }
                        }
                    }

                    return coupon;
                }, ReadStrategy.Wait);
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return null;
        }

        public UniqueCoupon GetByCouponCode(string code)
        {
            try
            {
                UniqueCoupon coupon = null;
                var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand
                    {
                        Connection = connection,
                        CommandType = CommandType.StoredProcedure,
                        CommandText = "UniqueCoupons_GetByCode"
                    };
                    command.Parameters.Add(new SqlParameter("@Code", code));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            coupon = GetUniqueCoupon(reader);
                        }
                    }
                }

                return coupon;
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return null;
        }

        public void RedeemCoupon(string code)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand
                    {
                        Connection = connection,
                        CommandType = CommandType.StoredProcedure,
                        CommandText = "UniqueCoupons_RedeemByCode"
                    };
                    command.Parameters.Add(new SqlParameter("@Code", code));
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }
        }

        public string GenerateCoupon()
        {
            return _couponCodeBuilder.Generate(new Options
            {
                Plaintext = "coupon-text"
            });
        }

        private DataTable CreateUniqueCouponsDataTable(IEnumerable<UniqueCoupon> coupons)
        {
            var tblUniqueCoupon = new DataTable();
            tblUniqueCoupon.Columns.Add(new DataColumn(IdColumn, typeof(long)));
            tblUniqueCoupon.Columns.Add(PromotionIdColumn, typeof(int));
            tblUniqueCoupon.Columns.Add(PromotionCodeColumn, typeof(string));
            tblUniqueCoupon.Columns.Add(CodeColumn, typeof(string));
            tblUniqueCoupon.Columns.Add(ValidColumn, typeof(DateTime));
            tblUniqueCoupon.Columns.Add(ExpirationColumn, typeof(DateTime));
            tblUniqueCoupon.Columns.Add(CustomerIdColumn, typeof(Guid));
            tblUniqueCoupon.Columns.Add(CreatedColumn, typeof(DateTime));
            tblUniqueCoupon.Columns.Add(MaxRedemptionsColumn, typeof(int));
            tblUniqueCoupon.Columns.Add(UsedRedemptionsColumn, typeof(int));

            foreach (var coupon in coupons)
            {
                var row = tblUniqueCoupon.NewRow();
                row[IdColumn] = coupon.Id;
                row[PromotionIdColumn] = coupon.PromotionId;
                row[PromotionCodeColumn] = coupon.PromotionCode;
                row[CodeColumn] = coupon.Code;
                row[ValidColumn] = coupon.ValidFrom;
                row[ExpirationColumn] = coupon.Expiration ?? (object)DBNull.Value;
                row[CustomerIdColumn] = coupon.CustomerId ?? (object)DBNull.Value;
                row[CreatedColumn] = coupon.Created;
                row[MaxRedemptionsColumn] = coupon.MaxRedemptions;
                row[UsedRedemptionsColumn] = coupon.UsedRedemptions;
                tblUniqueCoupon.Rows.Add(row);
            }

            return tblUniqueCoupon;
        }

        private void InvalidatePromotionCache(int id)
        {
            _cache.Remove(GetPromotionCacheKey(id));
        }

        private string GetPromotionCacheKey(int id)
        {
            return PromotionCachePrefix + id;
        }

        private void InvalidateCouponCache(long id)
        {
            _cache.Remove(GetCouponCacheKey(id));
        }

        private string GetCouponCacheKey(long id)
        {
            return CouponCachePrefix + id;
        }

        private CacheEvictionPolicy GetCacheEvictionPolicy(List<UniqueCoupon> coupons)
        {
            return new CacheEvictionPolicy(TimeSpan.FromHours(1), CacheTimeoutType.Absolute, coupons.Select(x => GetCouponCacheKey(x.Id)));
        }

        private int GetCount(IDataReader row)
        {
            return row[ReturnCountName] != DBNull.Value ? Convert.ToInt32(row[ReturnCountName]) : 0;
        }

        private UniqueCoupon GetUniqueCoupon(IDataReader row)
        {
            return new UniqueCoupon
            {
                Code = row[CodeColumn].ToString(),
                Created = Convert.ToDateTime(row[CreatedColumn]),
                CustomerId = row[CustomerIdColumn] != DBNull.Value ? (Guid?)new Guid(row[CustomerIdColumn].ToString()) : null,
                Expiration = row[ExpirationColumn] != DBNull.Value
                    ? (DateTime?)Convert.ToDateTime(row[ExpirationColumn].ToString())
                    : null,
                Id = Convert.ToInt64(row[IdColumn]),
                MaxRedemptions = Convert.ToInt32(row[MaxRedemptionsColumn]),
                PromotionId = Convert.ToInt32(row[PromotionIdColumn]),
                PromotionCode = row[PromotionCodeColumn].ToString(),
                UsedRedemptions = Convert.ToInt32(row[UsedRedemptionsColumn]),
                ValidFrom = Convert.ToDateTime(row[ValidColumn])
            };
        }
        public UniqueCoupon AssignCouponToCustomer(int id, Guid customerid)
        {
            if (customerid == null || customerid == Guid.Empty) return null;

            try
            {
                UniqueCoupon coupon = null;
                var connectionString = ConfigurationManager.ConnectionStrings[TRM.Shared.Constants.StringConstants.TrmCustomDatabaseName].ConnectionString;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand
                    {
                        Connection = connection,
                        CommandType = CommandType.StoredProcedure,
                        CommandText = "UniqueCoupons_AssignCouponToCustomer"
                    };
                    command.Parameters.Add(new SqlParameter("@CustomerId", customerid));
                    command.Parameters.Add(new SqlParameter("@Id", id));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            coupon = GetUniqueCoupon(reader);
                        }
                    }
                }

                return coupon;
            }
            catch (Exception exn)
            {
                _logger.Error(exn.Message, exn);
            }

            return null;
        }
    }
}