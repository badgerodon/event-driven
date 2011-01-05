using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Badgerodon.EventDriven
{
	/// <summary>
	/// Quick methods for running tasks
	/// </summary>
	public class Tasks
	{
		/// <summary>
		/// Run an action
		/// </summary>
		/// <param name="action"></param>
		public static void Do(Action action)
		{
			ThreadPool.QueueUserWorkItem(_ => action(), null);
		}

		/// <summary>
		/// Run an action after a delay
		/// </summary>
		/// <param name="action"></param>
		/// <param name="delay"></param>
		/// <returns></returns>
		public static Timer DoLater(Action action, int delay)
		{
			Timer timer = null;
			timer = new Timer(_ =>
			{
				Do(() =>
				{
					try
					{
						action();
					}
					finally
					{
						timer.Dispose();
					}
				});
			},
				null,
				delay,
				Timeout.Infinite
			);
			return timer;
		}
	}

}

