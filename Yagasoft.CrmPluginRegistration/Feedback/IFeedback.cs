using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yagasoft.CrmPluginRegistration.Feedback
{
    public interface IFeedback
    {
	    bool IsConfirmed(string message);
    }
}
