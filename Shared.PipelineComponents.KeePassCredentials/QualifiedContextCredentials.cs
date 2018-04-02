using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Shared.PipelineComponents
{
    public class QualifiedContextCredentials
    {
        public XmlQualifiedName Username { set; get; }
        public XmlQualifiedName Password { set; get; }
    }
}
