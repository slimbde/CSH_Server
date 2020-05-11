using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;


namespace residentsvc
{
	public partial class residentsvc : ServiceBase
	{
		public residentsvc( )
		{
			InitializeComponent( );
			eventLog1 = new EventLog( );
			if ( !System.Diagnostics.EventLog.SourceExists( "ResidentDebug" ) )
			{
				System.Diagnostics.EventLog.CreateEventSource(
					"ResidentDebug", "MyNewLog" );
			}
			eventLog1.Source = "ResidentDebug";
			eventLog1.Log = "MyNewLog";
		}

		protected override void OnStart( string[] args )
		{
			var myPath = AppDomain.CurrentDomain.BaseDirectory;
			try
			{
				if ( File.Exists( $"{myPath}svc.dll" ) )
					svc.Actions.Run( );
				else if ( File.Exists( $"{myPath}update.exe" ) )
				{
					var startInfo = new ProcessStartInfo( );
					startInfo.CreateNoWindow = true;
					startInfo.FileName = $"{myPath}update.exe";
					startInfo.WindowStyle = ProcessWindowStyle.Hidden;
				}
			}
			catch ( Exception ) { }
		}
	}
}
