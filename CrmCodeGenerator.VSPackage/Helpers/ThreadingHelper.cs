using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CrmPluginRegExt.VSPackage.Helpers
{

	public static class DispatcherEx
	{
		public static void InvokeOrExecute(this Dispatcher dispatcher, Action action)
		{
			if (dispatcher.CheckAccess())
			{
				action();
			}
			else
			{
				dispatcher.BeginInvoke(DispatcherPriority.Normal,
									   action);
			}
		}
	}
	internal class ThreadingHelper
	{
		private static Dispatcher dispatcher = System.Windows.Application.Current.Dispatcher;

	}
}
