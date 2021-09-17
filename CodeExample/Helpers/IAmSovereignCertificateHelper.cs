using TRM.Web.Constants;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Helpers
{
    public interface IAmSovereignCertificateHelper
    {
        Enums.eSovereignSignUpMessageStatus SaveSoverignSignup(SovereignCertificateSignUpDto signUp);
    }
}
