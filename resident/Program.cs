using System;
using System.IO;
using System.Diagnostics;
using resident.models;


namespace resident
{
	class Program
	{
		static void Main( string[] args )
		{
			try
			{
				if ( File.Exists( "features.dll" ) )
				{
					var resident = new Resident( );
					resident.Run( );
				}
				else
				{
					if ( File.Exists( "update.exe" ) )
						Process.Start( "update.exe" );
				}
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
