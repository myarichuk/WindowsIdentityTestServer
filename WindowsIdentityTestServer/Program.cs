using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;

namespace WindowsIdentityTestServer
{
	class Program
	{
		private static Thread listenerThread;
		private static readonly ManualResetEventSlim mre = new ManualResetEventSlim();
		private static readonly CancellationTokenSource cts = new CancellationTokenSource();
		private static HttpListener listener;

		static void Main(string[] args)
		{
			var port = ConfigurationManager.AppSettings["port"];
			listener = new HttpListener();
            using (listener)
			{
				listener.Prefixes.Add("http://+:" + port + "/");
				listener.AuthenticationSchemes = AuthenticationSchemes.IntegratedWindowsAuthentication;
				listener.Start();
				listenerThread = new Thread(_ => ProcessRequest());
				listenerThread.Start();

				Console.WriteLine("Test service to check what Windows credentials are being used.");
				Console.WriteLine("Issue a GET request to this server with a port " + port + " to see a user");
                Console.WriteLine("Accepting requests on port " + port + ", press enter to exit..");
				Console.ReadLine();
				mre.Set();
				cts.Cancel();
				listenerThread.Join(2000);
			}

		}

		private static void ProcessRequest()
		{
			while (!mre.IsSet)
			{
				try
				{
					var context = listener.GetContext();
					using (var outputWriter = new StreamWriter(context.Response.OutputStream))
					{
						outputWriter.Write("<div>Username = " + context.User.Identity.Name + "<br/>");
						outputWriter.Write("IsAuthenticated = " + context.User.Identity.IsAuthenticated + "<br/>");
						outputWriter.Write("AuthenticationType = " + context.User.Identity.AuthenticationType + "<br/></div>");
						outputWriter.Flush();
					}
				}
				catch(Exception e)
				{
					Console.WriteLine(e);
					Console.WriteLine("Error happened, stopped listening for requests");
					listener.Abort();
					break;
				}
			}
		}
	}
}
