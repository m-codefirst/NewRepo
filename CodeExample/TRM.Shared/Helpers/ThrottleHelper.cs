using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace TRM.Shared.Helpers
{
    [ServiceConfiguration(typeof(ThrottleHelper), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ThrottleHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ThrottleHelper));

        private readonly SemaphoreSlim _masterSemaphore;
        public const string ThrottleHelperCacheMasterKey = "ThrottleHelper_MasterKey";
        private readonly ISynchronizedObjectInstanceCache _cacheService;

        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _throttleTokenPool = new ConcurrentDictionary<string, CancellationTokenSource>();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphorePool = new ConcurrentDictionary<string, SemaphoreSlim>();

        public Lazy<double> DefaultTimeout = new Lazy<double>(() =>
        {
            var appConfig = WebConfigurationManager.AppSettings.Get("ThrottleHelperDefaultTimeout");
            var defaultTimeout = 1d; // 1 minute
            double.TryParse(appConfig, out defaultTimeout);

            return defaultTimeout;
        });

        private CacheEvictionPolicy _defaultCachePolicy = null;
        public CacheEvictionPolicy DefaultCachePolicy
        {
            get
            {
                if (_defaultCachePolicy == null)
                {
                    _defaultCachePolicy =
                        new CacheEvictionPolicy(
                            TimeSpan.FromMinutes(DefaultTimeout.Value),
                            CacheTimeoutType.Absolute,
                            new string[] { },
                            new string[] { ThrottleHelperCacheMasterKey });
                }

                return _defaultCachePolicy;
            }
        }

        public ThrottleHelper(ISynchronizedObjectInstanceCache cacheService)
        {
            _masterSemaphore = new SemaphoreSlim(1, 1);
            _cacheService = cacheService;
        }

        protected virtual Task<SemaphoreSlim> GetActionSemaphore(string actionKey)
        {
            return _masterSemaphore.WaitAsync().ContinueWith((t) =>
            {
                try
                {
                    if (!_semaphorePool.TryGetValue(actionKey, out SemaphoreSlim semaphore))
                    {
                        semaphore = new SemaphoreSlim(1, 1);
                    }

                    _semaphorePool[actionKey] = semaphore;
                    return semaphore;
                }
                finally
                {
                    _masterSemaphore.Release();
                }
            });
        }

        protected virtual Task<CancellationTokenSource> GetOrRenewToken(string actionKey)
        {
            var semaphore = GetActionSemaphore(actionKey).Result;

            return semaphore.WaitAsync().ContinueWith((t) =>
            {
                try
                {
                    if (_throttleTokenPool.TryGetValue(actionKey, out CancellationTokenSource source))
                    {
                        source.Cancel();
                    }

                    source = new CancellationTokenSource();
                    _throttleTokenPool[actionKey] = source;
                    return source;
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }

        /// <summary>
        /// Throttle write action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actionKey"></param>
        /// <param name="action"></param>
        public virtual Task<T> ExecuteLast<T>(string actionKey, Func<T> action)
        {
            var throttleTokenSource = GetOrRenewToken(actionKey).Result;

            try
            {
                var task = Task.Run(action);

                throttleTokenSource.Token.ThrowIfCancellationRequested();

                return task;
            }
            catch (OperationCanceledException ex)
            {
                if (ex is OperationCanceledException || throttleTokenSource.Token.IsCancellationRequested)
                    return Task.FromResult(default(T));

                Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Burst (1) and cache read action.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actionKey"></param>
        /// <param name="action"></param>
        /// <param name="cacheEvictionPolicy"></param>
        public virtual Task<T> ExecuteFirst<T>(string actionKey, Func<T> action, CacheEvictionPolicy cacheEvictionPolicy = null)
        {
            try
            {
                var semaphore = GetActionSemaphore(actionKey).Result;

                return semaphore.WaitAsync().ContinueWith<T>((t) =>
                {
                    try
                    {
                        var result = (T)_cacheService.Get(actionKey);
                        if (result != null)
                        {
                            return result;
                        }

                        result = action();

                        Task.Run(() => _cacheService.Insert(actionKey, result, cacheEvictionPolicy ?? DefaultCachePolicy));

                        return result;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Task.FromResult(default(T));
            }
        }
    }
}
