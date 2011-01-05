using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net;
using Badgerodon.EventDriven.Collections;

namespace Badgerodon.EventDriven.Network
{
	public abstract class TcpServer
	{
		public enum AfterSendAction
		{
			Nothing,
			Close
		}

		private Socket _listener;
		private Pool<SocketAsyncEventArgs> _acceptors;
		private Pool<SocketAsyncEventArgs> _receivers;
		private Pool<SocketAsyncEventArgs> _senders;
		
		public int Port { get; protected set; }

		public TcpServer()
		{
			Port = 8080;

			var bufferSize = 8192;
			var numberOfReceivers = 256;
			var numberOfSenders = 256;

			var buffer = new byte[bufferSize * numberOfReceivers];

			_acceptors = new Pool<SocketAsyncEventArgs>(
				Enumerable.Range(0, 10).Select(i =>
				{
					var saea = new SocketAsyncEventArgs();
					saea.Completed += (_, e) => ProcessAccept(e);
					return saea;
				})
			);

			_receivers = new Pool<SocketAsyncEventArgs>(
				Enumerable.Range(0, numberOfReceivers).Select(i =>
				{
					var saea = new SocketAsyncEventArgs();
					saea.Completed += (_, e) => ProcessReceive(e);
					saea.SetBuffer(buffer, i * bufferSize, bufferSize);
					return saea;
				})
			);

			_senders = new Pool<SocketAsyncEventArgs>(
				Enumerable.Range(0, numberOfSenders).Select(i =>
				{
					var saea = new SocketAsyncEventArgs();
					saea.Completed += (_, e) => ProcessSend(e);
					return saea;
				})
			);
		}

		/// <summary>
		/// Start the TCP server
		/// </summary>
		public virtual void Start()
		{
			_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_listener.Bind(new IPEndPoint(IPAddress.Any, Port));
			_listener.Listen(10000);

			// Start accepting connections
			Tasks.Do(StartAccept);
			Tasks.Do(StartAccept);
		}

		protected abstract void HandleRequest(Socket socket, BufferSegment data, ref object token);

		/// <summary>
		/// Accept a connection request
		/// </summary>
		protected void StartAccept()
		{
			Trace.TraceInformation("StartAccept");
			_acceptors.Take(acceptor =>
			{
				if (!_listener.AcceptAsync(acceptor))
				{
					ProcessAccept(acceptor);
				}
			});
		}

		/// <summary>
		/// Process an accepted connection
		/// </summary>
		/// <param name="acceptor"></param>
		protected void ProcessAccept(SocketAsyncEventArgs acceptor)
		{
			StartAccept();
			// Some sort of error
			if (acceptor.SocketError != SocketError.Success)
			{
				Close(acceptor.AcceptSocket);
			}
			else
			{
				StartReceive(acceptor.AcceptSocket, null);
			}

			// Give the acceptor back
			acceptor.AcceptSocket = null;
			acceptor.UserToken = null;
			_acceptors.Release(acceptor);
		}

		/// <summary>
		/// Start receiving data
		/// </summary>
		/// <param name="acceptor"></param>
		/// <param name="token"></param>
		protected void StartReceive(Socket acceptor, object token)
		{
			Trace.TraceInformation("StartReceive");
			_receivers.Take(receiver =>
			{
				if (acceptor.Connected)
				{
					receiver.AcceptSocket = acceptor;
					receiver.UserToken = token;

					if (!acceptor.ReceiveAsync(receiver))
					{
						ProcessReceive(receiver);
					}
				}
				else
				{
					_receivers.Release(receiver);
				}
			});
		}

		/// <summary>
		/// Process data received
		/// </summary>
		/// <param name="receiver"></param>
		protected void ProcessReceive(SocketAsyncEventArgs receiver)
		{
			var token = receiver.UserToken;
			var socket = receiver.AcceptSocket;
			bool cont = false;

			if (receiver.SocketError != SocketError.Success)
			{
				Close(socket);
			}
			else if (receiver.BytesTransferred > 0)
			{
				var segment = new BufferSegment(receiver.Buffer, receiver.Offset, receiver.BytesTransferred);
				HandleRequest(socket, segment, ref token);
				cont = true;
			}

			// Send the receiver back
			receiver.UserToken = null;
			receiver.AcceptSocket = null;
			_receivers.Release(receiver);

			if (cont)
			{
				StartReceive(socket, token);
			}
		}

		/// <summary>
		/// Start sending data
		/// </summary>
		/// <param name="acceptor"></param>
		/// <param name="data"></param>
		/// <param name="token"></param>
		public void StartSend(Socket acceptor, BufferSegment data, object token)
		{
			_senders.Take(sender =>
			{
				if (data.Length > 0 && acceptor.Connected)
				{
					sender.AcceptSocket = acceptor;
					sender.SetBuffer(data.Buffer, data.Offset, data.Length);
					sender.UserToken = token;

					if (!acceptor.SendAsync(sender))
					{
						ProcessSend(sender);
					}
				}
				else
				{
					_senders.Release(sender);
				}
			});
		}

		/// <summary>
		/// Process the send
		/// </summary>
		/// <param name="sender"></param>
		protected void ProcessSend(SocketAsyncEventArgs sender)
		{
			var token = sender.UserToken;

			if (sender.SocketError != SocketError.Success)
			{
				Close(sender.AcceptSocket);
			}
			else if (token is AfterSendAction)
			{
				var action = (AfterSendAction)token;
				if (action == AfterSendAction.Close)
				{
					Close(sender.AcceptSocket);
				}
			}

			// Send the sender back
			sender.UserToken = null;
			sender.AcceptSocket = null;
			_senders.Release(sender);
		}

		/// <summary>
		/// Close a socket
		/// </summary>
		/// <param name="socket"></param>
		protected void Close(Socket socket)
		{
			if (socket == null) return;

			try
			{
				socket.Shutdown(SocketShutdown.Both);
			}
			catch { }
			socket.Close();
		}
	}
}
