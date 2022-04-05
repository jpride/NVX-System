using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronDataStore;
using TSISignageApp.CrestronDataStore;

namespace TSISignageApp.CDS
{
	public class CDSInterface
	{
		public event EventHandler<SerialDataChangeEventArgs> SerialDataChangeEvent;


		public CDSInterface ( )
		{
			var error = CrestronDataStoreStatic.InitCrestronDataStore();

			if (error != Crestron.SimplSharp.CrestronDataStore.CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
			{
				CrestronConsole.PrintLine ( "Error Initializing Crestron Data Store" );
			}
		}

		public void Initialize ( )
		{
			var error = CrestronDataStoreStatic.InitCrestronDataStore();

			if (error != Crestron.SimplSharp.CrestronDataStore.CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
			{
				CrestronConsole.PrintLine ( "Error Initializing Crestron Data Store" );
			}
		}

		public void SetLocalStringValue ( string tag, string value )
		{
			var error = CrestronDataStoreStatic.SetLocalStringValue(tag, value);
			if (error == Crestron.SimplSharp.CrestronDataStore.CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
			{
				SerialDataChangeEventArgs args = new SerialDataChangeEventArgs()
				{
					tag = tag,
					value = value
				};

				SerialDataChangeEvent?.Invoke ( this, args );
			}
			else
			{
				CrestronConsole.PrintLine ( "Error {0} setting local value {1} for tag {2}", error, value, tag );
			}
		}

		public string GetLocalSerialValue ( string tag )
		{
			var error = CrestronDataStoreStatic.GetLocalStringValue(tag, out var value);

			if (error != Crestron.SimplSharp.CrestronDataStore.CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
			{
				CrestronConsole.PrintLine ( "Error {0} getting local value for tag {1}", error, tag );
			}

			return value;
		}
	}
}
