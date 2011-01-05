using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Badgerodon.EventDriven
{
	public class EventSource
	{
		private EventSource<bool> _source;

		public EventSource()
		{
			_source = new EventSource<bool>();
		}

		public void Bind(Action action)
		{
			_source.Bind(_ => action());
		}

		public void BindOnce(Action action)
		{
			_source.BindOnce(_ => action());
		}

		public void Trigger()
		{
			_source.Trigger(true);
		}
	}

	public class EventSource<T>
	{
		private class Entry
		{
			public Action<T> Action;
			public bool Rebind;
		}

		private object _lock;
		private object _triggerLock;
		private List<Entry> _bound;

		public EventSource()
		{
			_lock = new object();
			_triggerLock = new object();
			_bound = new List<Entry>();
		}

		public void Bind(Action<T> action)
		{
			lock (_lock)
			{
				_bound.Add(new Entry
				{
					Action = action,
					Rebind = true
				});
			}
		}

		public void BindOnce(Action<T> action)
		{
			lock (_lock)
			{
				_bound.Add(new Entry
				{
					Action = action,
					Rebind = false
				});
			}
		}

		public void Unbind(Action<T> action)
		{
			lock (_lock)
			{
				_bound = _bound.Where(e => e.Action != action).ToList();
			}
		}

		public void Trigger(T item)
		{
			lock (_triggerLock)
			{
				List<Entry> toTrigger = null;
				lock (_lock)
				{
					toTrigger = _bound.ToList();
					_bound = _bound.Where(e => e.Rebind).ToList();
				}

				foreach (Entry entry in toTrigger)
				{
					entry.Action(item);
				}
			}
		}
	}
}
