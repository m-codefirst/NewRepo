using EPiServer.Core;
using TRM.Web.Models.Blocks;

namespace TRM.Web.Helpers
{
    public interface ISiteHelper
    {
        bool StopTrading { get; }
        TrmHeaderMessageBlock TrmHeaderMessageBlock { get; }
        TrmImageBlock GetTrmImageBlock(ContentReference imageLink);
    }
}