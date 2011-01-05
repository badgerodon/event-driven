using System;
using System.Net.Sockets;
using Badgerodon.Web;

namespace Badgerodon.EventDriven.Network
{
	public class HttpServer : TcpServer, IHttpServer
	{
		public Action<HttpContext> OnRequest { get; protected set; }

		protected HttpServer(int port)
		{
			Port = port;
		}

		public HttpServer(int port, Action<HttpContext> onrequest)
			: this(port)
		{
			OnRequest = onrequest;
		}

		protected override void HandleRequest(Socket socket, BufferSegment data, ref object token)
		{
			HttpParser parser = (token as HttpParser) ?? new HttpParser();
			parser.AddData(data.Buffer, data.Offset, data.Length);

			if (parser.Completed)
			{
				OnRequest(new HttpContext(this, socket, parser.Request));
				token = null;
			}
			else
			{
				token = parser;
			}
		}

		public void Send(object handle, BufferSegment data)
		{
			StartSend(handle as Socket, data, null);
		}
	}
}
