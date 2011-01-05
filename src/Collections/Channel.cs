using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Badgerodon.EventDriven.Collections
{
	public class Channel<T>
	{
		private object _lock;
		private Queue<T> _backlog;
		private EventSource<T> _send;
		private EventSource _receive;

		public Channel()
		{
			_lock = new object();
			_backlog = new Queue<T>();
			_send = new EventSource<T>();
			_receive = new EventSource();

			_send.Bind(item =>
			{
				lock (_lock)
				{
					_backlog.Enqueue(item);
				}
				_receive.Trigger();
			});
		}

		public void Send(T item)
		{
			_send.Trigger(item);
		}

		public void Take(Action<T> action)
		{
			Action onSomething = null;

			onSomething = () =>
			{
				bool gotOne = false;
				T next = default(T);

				lock (_lock)
				{
					if (_backlog.Count > 0)
					{
						gotOne = true;
						next = _backlog.Dequeue();
					}
					else
					{
						_receive.BindOnce(onSomething);
					}
				}

				if (gotOne)
				{
					action(next);
				}
			};

			onSomething();
		}

		public void TakeAtLeastOne(Action<List<T>> action)
		{
			Action onSomething = null;

			onSomething = () =>
			{
				bool gotOne = false;
				List<T> next = new List<T>();

				lock (_lock)
				{
					if (_backlog.Count > 0)
					{
						gotOne = true;
						while (_backlog.Count > 0)
						{
							next.Add(_backlog.Dequeue());
						}
					}
					else
					{
						_receive.BindOnce(onSomething);
					}
				}

				if (gotOne)
				{
					action(next);
				}
			};

			onSomething();
		}
	}
}

