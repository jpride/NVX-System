using Crestron.SimplSharp;

namespace TSISignageApp.Logging
{
	

	public enum eDebugEventType
	{
		NA,
		Info,
		Notice,
		Ok,
		Warn,
		Error
	}

	public class Logging
	{
		private static bool _debug = false;

		public bool Debug
		{
			get { return _debug; }
			set { _debug = value; }
		}

		public static void OnDebug ( eDebugEventType eventType, string str, params object[ ] list )
		{
			if (_debug)
			{
				CrestronConsole.PrintLine ( str, list );
			}

			switch (eventType)
			{
				case eDebugEventType.Notice: ErrorLog.Notice ( str, list ); break;
				case eDebugEventType.Warn: ErrorLog.Warn ( str, list ); break;
				case eDebugEventType.Error: ErrorLog.Error ( str, list ); break;
			}
		}
	}
}
