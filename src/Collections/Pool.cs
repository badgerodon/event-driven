using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Badgerodon.EventDriven.Collections
{
	/// <summary>
	/// A fixed length pool of objects. If you try to take one that
	/// isn't available it will wait until one is available.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Pool<T>
	{
		private Channel<T> _free;

		/// <summary>
		/// Create a new pool of items
		/// </summary>
		/// <param name="items"></param>
		public Pool(IEnumerable<T> items)
		{
			_free = new Channel<T>();
			foreach (T item in items)
			{
				_free.Send(item);
			}
		}

		/// <summary>
		/// Take an object from the pool and pass it to action
		/// </summary>
		/// <param name="action"></param>
		public void Take(Action<T> action)
		{
			_free.Take(action);
		}

		/// <summary>
		/// Send the object back to the pool.
		/// </summary>
		/// <param name="item"></param>
		public void Release(T item)
		{
			_free.Send(item);
		}
	}
}

