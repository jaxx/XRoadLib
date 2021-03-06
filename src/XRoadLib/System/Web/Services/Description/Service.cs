#if NETSTANDARD1_6_1

using System.Collections.Generic;
using System.Xml;

namespace System.Web.Services.Description
{
    public class Service : NamedItem
    {
        protected override string ElementName { get; } = "service";

        public List<Port> Ports { get; } = new List<Port>();

        protected override void WriteElements(XmlWriter writer)
        {
            base.WriteElements(writer);
            Ports.ForEach(x => x.Write(writer));
        }
    }
}

#endif