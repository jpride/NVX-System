using System;


namespace TSISignageApp.CrestronDataStore
{
	public class SerialDataChangeEventArgs : EventArgs
	{
		public string tag { get; set; }
		public string value { get; set; }
	}

	public class GetGlobalSerialDataEventArgs : EventArgs
	{
		public string tag { get; set; }
		public string value { get; set; }
	}
}
