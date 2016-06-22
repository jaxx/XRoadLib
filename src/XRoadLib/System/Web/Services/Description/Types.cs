#if NETSTANDARD1_5

using System.Collections.Generic;
using XRoadLib.Xml.Schema;

namespace System.Web.Services.Description
{
    public class Types : DocumentableItem
    {
        public IList<XmlSchema> Schemas { get; } = new List<XmlSchema>();
    }
}

#endif