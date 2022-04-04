using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crestron.SimplSharp;

namespace TSISignageApp.HelperClasses
{
	public static class Debug
	{
		public  static bool _systemDebug = false;
		public static  bool _nvxDebug = false;
		public static  bool _uiDebug = false;

		public static bool nvxDebug
		{
			get { return _nvxDebug; }
			set { _nvxDebug = value; }
		}

		public static bool uiDebug
		{
			get { return _uiDebug; }
			set { _uiDebug = value; }
		}

		public static void SetNvxDebug ( string parms )
		{
			var input = parms.ToLower ();

			if (input == "?")
			{
				CrestronConsole.ConsoleCommandResponse ( $"Set Nvx Debug\n\rParameters: 'on' or 'off'" );
			}
			else
			{
				if (input.Equals ( "on" ))
				{
					nvxDebug = true;

				}
				else if (input.Equals ( "off" ))
				{
					nvxDebug = false;
				}
				CrestronConsole.ConsoleCommandResponse ( $"nvxDebug is {nvxDebug}" );
			}
		}

		public static void SetUiDebug ( string parms )
		{
			var input = parms.ToLower ();

			if (input == "?")
			{
				CrestronConsole.ConsoleCommandResponse ( $"Set UI Debug\n\rParameters: 'on' or 'off'" );
			}
			else
			{
				if (input.Equals ( "on" ))
				{
					uiDebug = true;
				}
				else if (input.Equals ( "off" ))
				{
					uiDebug = false;
				}
				CrestronConsole.ConsoleCommandResponse ( $"uiDebug is {uiDebug}" );
			}
		}

	}
}
