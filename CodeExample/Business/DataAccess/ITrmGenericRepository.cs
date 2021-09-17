using Hephaestus.CMS.DataAccess;
using Hephaestus.CMS.Models;

namespace TRM.Web.Business.DataAccess
{
    public interface ITrmGenericRepository<T> : IRepository<T> where T : DdsPocoBase
    {
    }
}
