using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace ZealousInnocence
{
    // <Operation Class="ZealousInnocence.PatchOperationMultiXpath">
    //   <xpath>
    //     <li>...</li>
    //     <li>...</li>
    //   </xpath>
    //   <value> ... </value>
    // </Operation>
    public class PatchOperationMultiXpath : PatchOperation
    {
        public List<string> xpath;   // its a list of xpaths, not a single one
        public XmlContainer value;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (xpath == null || value == null)
                return false;

            bool patched = false;

            foreach (var xp in xpath)
            {
                if (string.IsNullOrEmpty(xp))
                    continue;

                var nodes = xml.SelectNodes(xp);
                if (nodes == null)
                    continue;

                foreach (XmlNode node in nodes)
                {
                    foreach (XmlNode child in value.node.ChildNodes)
                    {
                        node.AppendChild(node.OwnerDocument.ImportNode(child, true));
                        patched = true;
                    }
                }
            }

            return patched;
        }
    }
}
