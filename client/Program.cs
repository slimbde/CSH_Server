using System;
using System.Threading;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace client
{
	class Program
	{
		static Socket clientSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
		static int port = 21000;


		static void Main( string[] args )
		{
			Connect( null );
			Console.ReadKey( );
		}

		private static void Connect( string ip )
		{
			if ( ip == null )
			{
				Console.Clear( );
				Console.Write( $"{DateTime.Now}\tclient has run. specify the ip address: " );
				ip = Console.ReadLine( );
			}

			int attempts = 0;
			while ( !clientSocket.Connected )
			{
				try
				{
					Thread.Sleep( 500 );
					++attempts;
					clientSocket.Connect( ip, port );
				}
				catch ( Exception ex )
				{
					Console.WriteLine( $"{DateTime.Now}\tconnection attempt #{attempts} Exception: {ex.Message}" );
				}
			}

			Console.WriteLine( $"{DateTime.Now}\tConnected to {ip} on port {port}" );
			Send( );
		}
		private static void Send( )
		{
			var ip = ( ( IPEndPoint )clientSocket.RemoteEndPoint ).Address.ToString( );

			while ( true )
			{
				string input;
				do
				{
					Console.Write( "> " );
					input = Console.ReadLine( );
				}
				while ( input.Length == 0 );

				byte[] buffer = Encoding.ASCII.GetBytes( input );

				try
				{
					clientSocket.Send( buffer );

					buffer = new byte[1024];
					int bytes = clientSocket.Receive( buffer );
					Array.Resize( ref buffer, bytes );
					string response = Encoding.ASCII.GetString( buffer );

					Console.WriteLine( $"{DateTime.Now}\t{ip} > {response}\n" );

					if ( response == "bye" )
					{
						Thread.Sleep( 3000 );

						clientSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
						Connect( null );
					}
					else if ( response == "updating" )
					{
						Thread.Sleep( 5000 );

						clientSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
						Connect( ip );
					}
				}
				catch ( SocketException ex )
				{
					Console.WriteLine( $"{DateTime.Now}\nserver disconnected exception: {ex.Message}" );
					Thread.Sleep( 5000 );

					clientSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
					Connect( null );
				}
			}
		}
	}
}
