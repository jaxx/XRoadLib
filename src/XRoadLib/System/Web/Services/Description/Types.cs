#if NETSTANDARD1_6_1

using System.Collections.Generic;
using System.Xml;
using XRoadLib.Xml.Schema;

namespace System.Web.Services.Description
{
    public class Types : DocumentableItem
    {
        protected override string ElementName { get; } = "types";

        public List<XmlSchema> Schemas { get; } = new List<XmlSchema>();

        protected override void WriteElements(XmlWriter writer)
        {
            base.WriteElements(writer);
            Schemas.ForEach(x => x.Write(writer));
        }
    }
}

#endif