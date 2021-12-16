#region Imports

using System;
using System.Threading.Tasks;

#endregion

namespace Yagasoft.CrmPluginRegistration.Logger
{
	public class DefaultPluginRegLogger : IPluginRegLogger
	{
		private readonly Action<string> logAction;

		public DefaultPluginRegLogger(Action<string> logAction)
		{
			this.logAction = logAction;
		}

		public void Log(string message)
		{
			if (logAction != null)
			{
				logAction(message);
			}
		}
	}
}
