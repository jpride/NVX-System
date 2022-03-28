using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.EthernetCommunication;

namespace NVX_System.HelperClasses
{
	 public class Eisc
	{

		private uint _ID;
		private string _ipAddress;

		EthernetIntersystemCommunications _eisc;

		public bool Online
		{
			get { return _eisc.IsOnline;  }
		}

		public event EventHandler<EiscEventArgs> _eiscEvent;

		public Eisc ( uint ID, string IPaddress, ControlSystem cs )
		{
			_eisc = new EthernetIntersystemCommunications ( ID, IPaddress, cs );
			_eisc.Register ( );

			_eisc.OnlineStatusChange += eisc_OnlineStatusChange;
			_eisc.SigChange += eisc_SigChange;

			_ID = ID; //store the ID we were set to
			_ipAddress = IPaddress; //store the IPaddress we used

			CrestronConsole.PrintLine ( "EISC Created" );
		}




		public void SetDigital ( uint Join, bool value )
		{
			_eisc.BooleanInput[ Join ].BoolValue = value;
		}

		public bool GetDigital ( uint Join )
		{
			return _eisc.BooleanOutput[ Join ].BoolValue;
		}

		public void SetAnalog ( uint Join, ushort Value )
		{ 
			_eisc.UShortInput[Join].UShortValue = Value;	
		}

		public ushort GetAnalog ( uint Join )
		{
			return _eisc.UShortOutput[ Join ].UShortValue;
		}

		public void SetSerial ( uint Join, string Value )
		{ 
			_eisc.StringInput[Join].StringValue = Value;	
		}
		public string GetSerial ( uint Join )
		{
			return _eisc.StringOutput[ Join ].StringValue;
		}




		private void eisc_SigChange ( BasicTriList currentDevice, SigEventArgs args )
		{
			OnRaiseEvent ( new EiscEventArgs ( "Signal", args ) );
		}

		private void eisc_OnlineStatusChange ( GenericBase currentDevice, OnlineOfflineEventArgs args )
		{
			CrestronConsole.PrintLine ( $"EISC is {(args.DeviceOnLine ? "online" : "Offline")}" );
		}

		protected virtual void OnRaiseEvent ( EiscEventArgs e )
		{
			//EventHandler<EiscEventArgs> raiseEvent = _eiscEvent;

			if (_eiscEvent != null)
			{
				e.Online = Online;
				e.ID = _ID;
				e.IpAddress = _ipAddress;

				_eiscEvent ( this, e);
			}
		}
	}

	public class EiscEventArgs
	{
		public string Message { get; set; }

		public SigEventArgs Args { get; set; }

		public bool Online { get; set; }

		public uint ID { get; set; }

		public string IpAddress { get; set; }


		public EiscEventArgs ( string message )
		{
			Message = message;
		}

		public EiscEventArgs ( string message, SigEventArgs args )
		{ 
			Message = message;
			Args = args;
		}
	}
}
