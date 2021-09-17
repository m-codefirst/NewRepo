using System;
using System.Security.Principal;
using System.Threading;
using EPiServer.Security;

namespace TRM.Web.Business.User
{
    public abstract class PrincipalScope : IDisposable
    {
        private readonly IPrincipal originalThreadPrincipal;
        private readonly IPrincipal originalPrincipalInfo;
        private readonly IUserImpersonation userImpersonation;

        protected PrincipalScope(string userName, IUserImpersonation userImpersonation)
        {
            this.userImpersonation = userImpersonation;
            this.originalThreadPrincipal = Thread.CurrentPrincipal;
            if (IsInEpiserverScope)
            {
                this.originalPrincipalInfo = PrincipalInfo.CurrentPrincipal;
            }
            this.SwapCurrentPrincipal(userName);
        }

        private void SwapCurrentPrincipal(string user)
        {
            if (string.IsNullOrEmpty(user))
            {
                throw new ArgumentException("Username cannot be empty.");
            }

            var currentPrincipal = userImpersonation.CreatePrincipal(user);

            Thread.CurrentPrincipal = currentPrincipal;
            if (IsInEpiserverScope)
            {
                PrincipalInfo.CurrentPrincipal = currentPrincipal;
            }
        }

        private static bool IsInEpiserverScope
        {
            get
            {
                try
                {
                    var settings = EPiServer.Configuration.Settings.Instance;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            Thread.CurrentPrincipal = this.originalThreadPrincipal;
            if (IsInEpiserverScope)
            {
                PrincipalInfo.CurrentPrincipal = this.originalPrincipalInfo;
            }
        }
    }
}