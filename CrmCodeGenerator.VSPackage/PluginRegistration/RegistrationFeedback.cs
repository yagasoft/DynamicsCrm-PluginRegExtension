#region Imports

using System;
using CrmPluginRegExt.VSPackage.Helpers;
using Yagasoft.CrmPluginRegistration.Feedback;

#endregion

namespace CrmPluginRegExt.VSPackage.PluginRegistration
{
	internal class RegistrationFeedback : IFeedback
	{
		public bool IsConfirmed(string message)
		{
			return DteHelper.IsConfirmed(message, "Confirmation ...");
		}
	}
}
