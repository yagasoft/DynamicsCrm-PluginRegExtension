using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrmPluginRegExt.VSPackage.Model
{
    [Serializable]
    public class Context
    {
        public string Namespace { get; set; }

		public string FileName { get; set; }

		public bool SplitFiles { get; set; }

        public MappingEntity[] Entities { get; set; }

		public MappingAction[] GlobalActions { get; set; }
    }
}
