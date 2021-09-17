using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TRM.Web.Helpers
{
   public interface IAmXmlHelper
    {
        XmlDocument Serialize<T>(T source);
    }
}
