using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;
using System;

namespace screensvc
{
	public partial class screensvc : ServiceBase
	{
		public screensvc( )
		{
			InitializeComponent( );

			eventLog1 = new EventLog( );
			if ( !EventLog.SourceExists( "MySource" ) )
				EventLog.CreateEventSource( "MySource", "screensvc" );
			eventLog1.Source = "MySource";
			eventLog1.Log = "screensvc";
		}

		protected override void OnStart( string[] args )
		{
			var timer = new System.Timers.Timer( );
			timer.Interval = 10000;
			timer.Elapsed += new ElapsedEventHandler( ( obj, ags ) =>
			{
				if ( Process.GetProcessesByName( "resident" ).Length == 0 )
				{
					var startInfo = new ProcessStartInfo( );
					startInfo.CreateNoWindow = true;
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
					startInfo.FileName = $"cmd.exe /C start {AppDomain.CurrentDomain.BaseDirectory.Replace( "Screen", "Defender" )}resident.exe";
					//startInfo.Arguments = $"/C start \"{AppDomain.CurrentDomain.BaseDirectory.Replace( "Screen", "Defender" )}resident.exe\"";
					//startInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory.Replace( "Screen", "Defender" )}resident.exe";
					//startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory.Replace( "Screen", "Defender" );

					eventLog1.WriteEntry( startInfo.FileName );
					//eventLog1.WriteEntry( startInfo.WorkingDirectory );
					eventLog1.WriteEntry( startInfo.Arguments );
					Process.Start( startInfo );
				}
			} );
			timer.Start( );
		}
	}
}
