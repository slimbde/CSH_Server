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
				foreach ( var process in Process.GetProcessesByName( "resident" ) )
					process.Kill( );

				var src = "http://slimbde.atwebpages.com/share/src/features.dll";
				var dst = "features.dll";

				System.Threading.Thread.Sleep( 2000 );

				var webClient = new WebClient( );
				webClient.DownloadFile( src, dst );

				var startInfo = new ProcessStartInfo( );
				startInfo.FileName = "resident.exe";
				startInfo.CreateNoWindow = true;
				//startInfo.WindowStyle = ProcessWindowStyle.Hidden;
				Process.Start( startInfo );
			}
			catch ( Exception ex )
			{
				var str = string.Empty;
				if ( ex.Message != "" )
					str += string.Concat( "exception: ", ex.Message, "\n" );

				if ( ex.InnerException.Message != "" )
					str += string.Concat( "innerException: ", ex.InnerException.Message );

				Console.WriteLine( str );
			}
		}
	}
}
