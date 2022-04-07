namespace TSISignageApp
{
	public enum PanelSmartObjectIDs
	{
		MainNavList = 1, //button list
		SourceSelectionList = 21, //Source SRL
		DestSelectList = 22, //Dest SRL
		NvxInfoList = 4, //NVX SRL
		SourceNameList = 5, //Source name list
		DestNameList = 6, //dest name list
		CafeAudioSrcList = 10, 
		Office1AudioSrcList = 11,
		Office2AudioSrcList = 12,
		CorridorAudioSrcList = 13,
		LobbyAudioSrcList = 14, //
	};


	public enum PageJoins
	{ 
		Routing = 10,
		Audio = 11,
		Naming = 12,	
	};

	public class NvxIpids
	{
		public uint lobbyIpid = 0x21;
		public uint engNorthIpid = 0x22;
		public uint engEastIpid = 0x23;
		public uint engWestIpid = 0x24;
		public uint serviceNorthIpid = 0x25;
		public uint serviceWestIpid = 0x26;
	}

	public class Joins
    {
        public uint RoutingSelectAll = 20;
		public uint RoutingClearAll = 21;
		public uint RoutingNvxInfo = 100;
		public uint RoutingNvxInfoExit = 101;

	}
}

