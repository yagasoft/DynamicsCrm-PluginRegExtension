using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace Yagasoft.CrmPluginRegistration.Connection
{
    public interface IConnectionManager
    {
	    IOrganizationService Get();
	    void ClearCache();
    }
}
