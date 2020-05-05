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
			Connect( );
			Console.ReadKey( );
		}

		private static void Connect( )
		{
			Console.Clear( );
			Console.Write( $"{DateTime.Now}\tclient has run. specify the ip address: " );
			var ip = Console.ReadLine( );

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

					if ( response == "bye" )
						throw new SocketException( 10054 );
					else if ( response == "updating" )
						throw new SocketException( 10055 );

					Console.WriteLine( $"{DateTime.Now}\t{ip} > {response}\n" );
				}
				catch ( SocketException ex )
				{
					Console.WriteLine( $"{DateTime.Now}\nserver disconnected exception: {ex.Message}" );
					Thread.Sleep( 5000 );

					if ( ex.ErrorCode == 10055 )
					{
						clientSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
						Console.Clear( );

						var attempts = 0;
						while ( !clientSocket.Connected )
						{
							try
							{
								Thread.Sleep( 500 );
								++attempts;
								clientSocket.Connect( ip, port );
							}
							catch ( Exception exCon )
							{
								Console.WriteLine( $"{DateTime.Now}\tconnection attempt #{attempts} Exception: {exCon.Message}" );
							}
						}

						Console.WriteLine( $"{DateTime.Now}\t: Connected to {ip} on port {port}" );
						Send( );

						return;
					}

					clientSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
					Connect( );
				}
			}
		}
	}
}
