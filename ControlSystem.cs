using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM.Streaming;
using Crestron.SimplSharpPro.UI;
using System;
using System.Collections.Generic;
using System.Threading;

using TSISignageApp.HelperClasses;
using TSISignageApp.CDS;
using SG = TSISignageApp.UI.SmartGraphicsHelper;
using UI = TSISignageApp.UI.UserInterfaceHelper;



namespace TSISignageApp
{
	public class ControlSystem : CrestronControlSystem
    {
		#region properties and variables
		private string sgdFileName = "SignageXPanelExe.sgd";

		public NvxIpids nvxIpids = new NvxIpids();
		public Joins joins = new Joins();

		CDSInterface cdsInt = new CDSInterface();

		//devices
		private  XpanelForSmartGraphics _xpanel;

		//nvx decoders
		private  DmNvxD30 _lobbyRx;
		private  DmNvxD30 _engNorthRx;
		private  DmNvxD30 _engEastRx;
		private  DmNvxD30 _engWestRx;
		private  DmNvxD30 _serviceNorthRx;
		private  DmNvxD30 _serviceWestRx;

		public enum NvxRx
		{ 
			lobby = 1,
			engNorth = 2,
			engEast = 3,
			engWest = 4,
			serviceNorth = 5,
			serviceWest = 6,
		};

		

		//List of nvx encoders
		private List<DmNvxE30> _nvxTxList = new List<DmNvxE30>();


		//logic vars
		private ushort _sourceSelection; //1 based index of current source selection
		private ushort _nav; //var to keep track of ui page
							 //private ushort _audioNav; //var to keep track of audio ui page
		#endregion


		//Control System ctor
		public ControlSystem () : base()
        {
            
            Crestron.SimplSharpPro.CrestronThread.Thread.MaxNumberOfUserThreads = 20;

			ConfigureUserInterfaces ( );
			ConfigureNvxDevices ( );

			
        }

		//initialize system
		public override void InitializeSystem ( )
		{
			try
			{
				var programThread = new Thread(() =>
				{
                    //Program Entrypoint
                    ConfigureTswSettings(_xpanel);

					ConfigureNvxSettings(_lobbyRx);
					ConfigureNvxSettings(_engNorthRx);
					ConfigureNvxSettings(_engEastRx);
					ConfigureNvxSettings(_engWestRx);
					ConfigureNvxSettings(_serviceNorthRx);
					ConfigureNvxSettings(_serviceWestRx);

					int i = 0;
					foreach (var tx in _nvxTxList)
					{
						ConfigureNvxSettings(tx);
						i++;
					}

					CrestronConsole.AddNewConsoleCommand (
						Debug.SetNvxDebug,
						"setnvxdebug",
						"Sets nvx debug on control system",
						ConsoleAccessLevelEnum.AccessOperator );

					CrestronConsole.AddNewConsoleCommand (
						Debug.SetUiDebug,
						"setuidebug",
						"Sets ui debug on control system",
						ConsoleAccessLevelEnum.AccessOperator );
				} );

				programThread.Start ( );

			}
			catch (Exception e)
			{
				CrestronConsole.PrintLine ( $"Error in InitializeSystem: {e.Message}" );
			}
		}

		//********************** UI Event Handlers ********************************// 

		void UI_SmartObject_SigChange ( GenericBase currentDevice, SmartObjectEventArgs args )
		{
			var dev = (BasicTriListWithSmartObject)currentDevice;
			SmartObject so = dev.SmartObjects[args.SmartObjectArgs.ID];
			Sig sig = args.Sig;
			
			switch (args.SmartObjectArgs.ID)
			{
				case (uint)PanelSmartObjectIDs.MainNavList: UI_SmartObject_Nav_SigChange ( dev, args ); break;
				case (uint)PanelSmartObjectIDs.SourceSelectionList: UI_SourceSrlChange ( dev, args ); break;
				case (uint)PanelSmartObjectIDs.DestSelectList: UI_DestSrlChange ( dev, args ); break;
				case (uint)PanelSmartObjectIDs.SourceNameList: UI_SourceNameListChange ( dev, args ); break;
				case (uint)PanelSmartObjectIDs.DestNameList: UI_DestNameListChange ( dev, args ); break;
				case (uint)PanelSmartObjectIDs.NvxInfoList: break;
			}
		}

		private void UI_SmartObject_Nav_SigChange ( BasicTriListWithSmartObject currentDevice, SmartObjectEventArgs args )
		{
			SmartObject so = currentDevice.SmartObjects[args.SmartObjectArgs.ID];
			Sig sig = args.Sig;

			switch (sig.Type)
			{
				case eSigType.Bool:
					if (sig.BoolValue) //press
					{
						CrestronConsole.PrintLine ( $"nav handler: {sig.UShortValue}" );
						switch (sig.Number)
						{
							default:
								if (Debug.uiDebug) CrestronConsole.PrintLine ( $"Nav Switch\nsig.Number: {sig.Number}" );
								_nav = (ushort)(sig.Number - 10);

								string buttonText = "Item " + sig.Number.ToString();
								SG.SetSmartObjectDigitalJoin ( so, (int)(sig.Number), true );
								
								if (Debug.uiDebug) CrestronConsole.PrintLine ( $"_nav: {_nav}" );
								UI_UpdatePage ( );
								break;
						}
					}
					else //release
					{
						SG.SetSmartObjectDigitalJoin ( so, (int)(sig.Number), false );
					}
					break;
			}
		}

		private void UI_SourceSrlChange ( BasicTriListWithSmartObject currentDevice, SmartObjectEventArgs args )
		{
			if (Debug.uiDebug) SigHelper.CheckSigProperties ( args.Sig );

			if (args.Sig.Type == eSigType.UShort) 
			{
				_sourceSelection = args.Sig.UShortValue;
				UI_UpdateSourceListFb ( currentDevice, args );
			}
		}

		private void UI_DestSrlChange ( BasicTriListWithSmartObject currentDevice, SmartObjectEventArgs args )
		{
			if (Debug.uiDebug)  CrestronConsole.PrintLine ( "Destination Selection" );
			if (Debug.uiDebug)  SigHelper.CheckSigProperties ( args.Sig );

			if (args.Sig.Type == eSigType.UShort)
			{
				var _destSelection = args.Sig.UShortValue;
				if (Debug.uiDebug)  CrestronConsole.PrintLine ( $"Destination: {_destSelection}" );
				UpdateRoute (_destSelection);

				UpdateActiveRouteIndicators ( currentDevice, args );
			}
		}

		private void UI_UpdateSourceListFb ( BasicTriListWithSmartObject currentDevice, SmartObjectEventArgs args)
		{
			SmartObject so = currentDevice.SmartObjects[args.SmartObjectArgs.ID];

			if (Debug.uiDebug) CrestronConsole.PrintLine ( $"Updating Source List Feedback: SourceSelection: {_sourceSelection}" );
			
			for (ushort i = 4011; i <= 4025; i += 2) //clear all source button feedback first
			{
				//_xpanel.SmartObjects[ (uint)(PanelSmartObjectIDs.SourceSelectionList) ].BooleanInput[ i ].BoolValue = false;
				UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, i, false );
			}

			switch (_sourceSelection)
			{
				case 1:
					UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, 4011, true );
					break;
				case 2:
					UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, 4013, true );
					break;
				case 3:
					UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, 4015, true );
					break;
				case 4:
					UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, 4017, true );
					break;
				case 5:
					UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, 4019, true );
					break;
				case 6:
					UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, 4021, true );
					break;
				case 7:
					UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, 4023, true );
					break;
				case 8:
					UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, 4025, true );
					break;
				default:
					break;
			}

			UpdateActiveRouteIndicators ( currentDevice, args);
		}

		public void UI_SigChange ( BasicTriList currentDevice, SigEventArgs args )
		{
			//**************************************************************
			//TSW_UI is a static class for handling SigEvents from various events
			//TSW_UI.ProcessSigChange(currentDevice, args);   
			//**************************************************************


			SigHelper.CheckSigProperties ( args.Sig );

			//the SigHelper CheckSignalPoperties Consoles out helpful info about the incoming signals
			//SigHelper.CheckSigProperties(args.Sig);

			if (args.Sig.Type == eSigType.Bool)
			{
				if (args.Sig.Number == joins.RoutingSelectAll && args.Sig.BoolValue)
				{
					if (!_sourceSelection.Equals ( 0 ))
					{
						UpdateRoute ( (ushort)NvxRx.lobby );
						UpdateRoute ( (ushort)NvxRx.engNorth );
						UpdateRoute ( (ushort)NvxRx.engEast );
						UpdateRoute ( (ushort)NvxRx.engWest );
						UpdateRoute ( (ushort)NvxRx.serviceNorth );
						UpdateRoute ( (ushort)NvxRx.serviceWest );

						//UpdateActiveRouteIndicators ( currentDevice, args);
					}
				}
				else if (args.Sig.Number == joins.RoutingClearAll && args.Sig.BoolValue)
				{
					_sourceSelection = 0;

					UpdateRoute ( (ushort)NvxRx.lobby );
					UpdateRoute ( (ushort)NvxRx.engNorth );
					UpdateRoute ( (ushort)NvxRx.engEast );
					UpdateRoute ( (ushort)NvxRx.engWest );
					UpdateRoute ( (ushort)NvxRx.serviceNorth );
					UpdateRoute ( (ushort)NvxRx.serviceWest );

					//UpdateActiveRouteIndicators ( currentDevice, args );
				}
				else if (args.Sig.Number == joins.RoutingNvxInfo && args.Sig.BoolValue)
				{
					UI.UserInterfaceHelper.SetDigitalJoin ( currentDevice, joins.RoutingNvxInfo, true );
					//_xpanel.BooleanInput[ joins.RoutingNvxInfo ].BoolValue = true;
				}
				else if (args.Sig.Number == joins.RoutingNvxInfoExit && args.Sig.BoolValue)
				{
					UI.UserInterfaceHelper.SetDigitalJoin ( currentDevice, joins.RoutingNvxInfo, false );
					//_xpanel.BooleanInput[ joins.RoutingNvxInfo ].BoolValue = false;
				}
			}
		}

		private void UI_SourceNameListChange ( BasicTriListWithSmartObject dev, SmartObjectEventArgs args )
		{
			SmartObject so = dev.SmartObjects[args.SmartObjectArgs.ID];

			if (Debug.uiDebug) SigHelper.CheckSigProperties ( args.Sig );

			var index = args.Sig.UShortValue - 1;
			var stringSigNum = args.Sig.Number;

			var nameData = "";
			var routeIndex = 0;
			var joinData = 0;
			var queueChanges = false;
			var cdsTag = "";

			switch (args.Sig.Number)
			{
				case 12:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 0;
					joinData = 11;
					cdsTag = "Source1Name";
					queueChanges = true;
					break;
				case 14:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 1;
					joinData = 12;
					cdsTag = "Source2Name";
					queueChanges = true;
					break;
				case 16:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 2;
					joinData = 13;
					cdsTag = "Source3Name";
					queueChanges = true;
					break;
				case 18:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 3;
					joinData = 14;
					cdsTag = "Source4Name";
					queueChanges = true;
					break;
				case 20:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 4;
					joinData = 15;
					cdsTag = "Source5Name";
					queueChanges = true;
					break;
				case 22:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 5;
					joinData = 16;
					cdsTag = "Source6Name";
					queueChanges = true;
					break;
				case 24:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 6;
					joinData = 17;
					cdsTag = "Source7Name";
					queueChanges = true;
					break;
				case 26:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 7;
					joinData = 18;
					cdsTag = "Source8Name";
					queueChanges = true;
					break;
			}

			if (queueChanges)
			{
				SmartObject sourceSelectList = dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceSelectionList ];
				VideoRoutes.routes[ routeIndex ].name = nameData;
				cdsInt.SetLocalStringValue ( cdsTag, VideoRoutes.routes[ routeIndex ].name );
				var formattedName = UI.UserInterfaceHelper.FormatTextForUi(VideoRoutes.routes[ routeIndex ].name,28,UI.UserInterfaceHelper.eCrestronFont.Arial,UI.UserInterfaceHelper.eNamedColour.White);
				UI.SmartGraphicsHelper.SetSmartObjectTextByJoin ( sourceSelectList, (ushort)joinData, formattedName );
			}
		}

		private void UI_DestNameListChange ( BasicTriListWithSmartObject dev, SmartObjectEventArgs args )
		{
			SmartObject so = dev.SmartObjects[args.SmartObjectArgs.ID];

			if (Debug.uiDebug) SigHelper.CheckSigProperties ( args.Sig );

			var nameData = "";
			var routeIndex = 0;
			var joinData = 0;
			var queueChanges = false;
			var cdsTag = "";


			switch (args.Sig.Number)
			{
				case 12:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 0;
					joinData = 11;
					cdsTag = "Dest1Name";
					queueChanges = true;
					break;
				case 14:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 1;
					joinData = 13;
					cdsTag = "Dest2Name";
					queueChanges = true;
					break;
				case 16:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 2;
					joinData = 15;
					cdsTag = "Dest3Name";
					queueChanges = true;
					break;
				case 18:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 3;
					joinData = 17;
					cdsTag = "Dest4Name";
					queueChanges = true;
					break;
				case 20:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 4;
					joinData = 19;
					cdsTag = "Dest5Name";
					queueChanges = true;
					break;
				case 22:
					nameData = dev.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringOutput[ args.Sig.Number ].StringValue;
					routeIndex = 5;
					joinData = 21;
					cdsTag = "Dest6Name";
					queueChanges = true;
					break;
			}

			if (queueChanges) //only act if a change was made
			{
				SmartObject destSelectList = dev.SmartObjects[(uint)PanelSmartObjectIDs.DestSelectList];
				VideoRoutes.destinations[ routeIndex ].name = nameData;
				cdsInt.SetLocalStringValue ( cdsTag, VideoRoutes.destinations[ routeIndex ].name );
				var formattedName = UI.UserInterfaceHelper.FormatTextForUi(VideoRoutes.destinations[ routeIndex ].name,28,UI.UserInterfaceHelper.eCrestronFont.Arial,UI.UserInterfaceHelper.eNamedColour.White);
				UI.SmartGraphicsHelper.SetSmartObjectTextByJoin ( destSelectList, (ushort)joinData, formattedName );
			}
		}


		//*************Configure devices at Initialization**************

		void ConfigureUserInterfaces ( )
		{
			_xpanel = new XpanelForSmartGraphics ( 0x34, this ) { Description = "Xpanel" };

			ConfigureUserInterface ( _xpanel );
		}

		void ConfigureUserInterface ( BasicTriListWithSmartObject currentDevice )
		{
			var dev = currentDevice;

			dev.Register ( );
			dev.OnlineStatusChange += XpanelOnlineStatusChange;
			dev.IpInformationChange += XpanelIpInformationChange;
			dev.SigChange += UI_SigChange; //this eventhandler handles ALL panel signal events


			dev.SmartObjects[ (uint)PanelSmartObjectIDs.MainNavList ].SigChange += UI_SmartObject_SigChange; //this handles only events coming from SmartObject ID 2 (PanelSmartObjectIDs.NavList)
			dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceSelectionList ].SigChange += UI_SmartObject_SigChange;
			dev.SmartObjects[ (uint)PanelSmartObjectIDs.DestSelectList ].SigChange += UI_SmartObject_SigChange;
			dev.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].SigChange += UI_SmartObject_SigChange;
			dev.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].SigChange += UI_SmartObject_SigChange;

			LoadUserInterfacesSmartGraphics ( dev );
		}

		void ConfigureNvxDevices ( )
		{

			_lobbyRx = new DmNvxD30 ( nvxIpids.lobbyIpid, this ) { Description = "Lobby Decoder" };
			_engNorthRx = new DmNvxD30 ( nvxIpids.engNorthIpid, this ) { Description = "Eng North Decoder" };
			_engEastRx = new DmNvxD30 ( nvxIpids.engEastIpid, this ) { Description = "Eng East Decoder" };
			_engWestRx = new DmNvxD30 ( nvxIpids.engWestIpid, this ) { Description = "Eng West Decoder" };
			_serviceNorthRx = new DmNvxD30 ( nvxIpids.serviceNorthIpid, this ) { Description = "Service North Decoder" };
			_serviceWestRx = new DmNvxD30 ( nvxIpids.serviceWestIpid, this ) { Description = "Service West Decoder" };


			_nvxTxList.Add ( new DmNvxE30 ( 0x0B, this ) { Description = $"Nvx-Tx-1" } );
			_nvxTxList.Add ( new DmNvxE30 ( 0x0C, this ) { Description = $"Nvx-Tx-2" } );
			_nvxTxList.Add ( new DmNvxE30 ( 0x0D, this ) { Description = $"Nvx-Tx-3" } );
			_nvxTxList.Add ( new DmNvxE30 ( 0x0E, this ) { Description = $"Nvx-Tx-4" } );
			_nvxTxList.Add ( new DmNvxE30 ( 0x0F, this ) { Description = $"Nvx-Tx-5" } );
			_nvxTxList.Add ( new DmNvxE30 ( 0x10, this ) { Description = $"Nvx-Tx-6" } );
			_nvxTxList.Add ( new DmNvxE30 ( 0x11, this ) { Description = $"Nvx-Tx-7" } );
			_nvxTxList.Add ( new DmNvxE30 ( 0x12, this ) { Description = $"Nvx-Tx-8" } );

			ConfigureNvxDevice ( _lobbyRx );
			ConfigureNvxDevice ( _engNorthRx );
			ConfigureNvxDevice ( _engEastRx );
			ConfigureNvxDevice ( _engWestRx );
			ConfigureNvxDevice ( _serviceNorthRx );
			ConfigureNvxDevice ( _serviceWestRx );

			foreach (var tx in _nvxTxList)
			{
				ConfigureNvxDevice ( tx );
			}
		}

		void LoadUserInterfacesSmartGraphics ( BasicTriListWithSmartObject currentDevice )
		{
			try
			{
				string SDGFilePath = Path.Combine(Directory.GetApplicationDirectory(), sgdFileName);
				CrestronConsole.PrintLine ( $"SDGFilePath: {SDGFilePath}" );

				if (File.Exists ( SDGFilePath ))
				{
					currentDevice.LoadSmartObjects ( SDGFilePath );
					ErrorLog.Notice ( $"SGD File loaded!" );
				}
				else
				{
					ErrorLog.Error ( "SmartGraphics Definition file not found! Set .sgd file to 'Copy Always'!" );
				}

				foreach (KeyValuePair<uint, SmartObject> kvp in currentDevice.SmartObjects)
				{

					SG.PrintSmartObjectSigNames ( kvp.Value );
				}

			}
			catch (Exception e)
			{
				ErrorLog.Error ( $"Error loading smartgraphics definition: {e.Message}" );
			}



		}



		void ConfigureNvxDevice ( DmNvxBaseClass device )
		{
			device.Register ( );
		}

		public void ConfigureNvxSettings(DmNvxBaseClass nvx)
        {
            try
            {  
                nvx.IpInformationChange += NvxIpInformationChangeEvent;
                nvx.Control.EnableAutomaticInitiation();
                nvx.Control.Start();


				var supportsSourceTransmit = true;
				var supportsSourceReceive = true;

				if (nvx.SourceTransmit is null)
				{
					supportsSourceTransmit = false;
				}
				if (nvx.SourceReceive is null)
				{
					supportsSourceReceive = false;
				}

				if (supportsSourceTransmit)
				{
					nvx.SourceTransmit.StreamChange += NvxTxStreamChangeEvent;
					nvx.OnlineStatusChange += NvxTxOnlineStatusChangeEvent;
				}

				if (supportsSourceReceive)
				{ 
					nvx.SourceReceive.StreamChange += NvxRxStreamChangeEvent;
					nvx.OnlineStatusChange += NvxRxOnlineStatusChangeEvent;
				}
				
			}
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"ConfigureNVX() Error for {nvx.Description}: {e.Message}");
            }
        }

		public void ConfigureTswSettings ( BasicTriList device ) //this method places text throughout the panel 
		{
			//populate the dest and source list names from CDS-----------------------------------------------------------------------------------------------
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestSelectList ].StringInput[ 11 ].StringValue = UI.UserInterfaceHelper.FormatTextForUi ( cdsInt.GetLocalSerialValue ( "Dest1Name" ), 28, UI.UserInterfaceHelper.eCrestronFont.Arial, UI.UserInterfaceHelper.eNamedColour.Silver );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestSelectList ].StringInput[ 13 ].StringValue = UI.UserInterfaceHelper.FormatTextForUi ( cdsInt.GetLocalSerialValue ( "Dest2Name" ), 28, UI.UserInterfaceHelper.eCrestronFont.Arial, UI.UserInterfaceHelper.eNamedColour.Silver );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestSelectList ].StringInput[ 15 ].StringValue = UI.UserInterfaceHelper.FormatTextForUi ( cdsInt.GetLocalSerialValue ( "Dest3Name" ), 28, UI.UserInterfaceHelper.eCrestronFont.Arial, UI.UserInterfaceHelper.eNamedColour.Silver );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestSelectList ].StringInput[ 17 ].StringValue = UI.UserInterfaceHelper.FormatTextForUi ( cdsInt.GetLocalSerialValue ( "Dest4Name" ), 28, UI.UserInterfaceHelper.eCrestronFont.Arial, UI.UserInterfaceHelper.eNamedColour.Silver );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestSelectList ].StringInput[ 19 ].StringValue = UI.UserInterfaceHelper.FormatTextForUi ( cdsInt.GetLocalSerialValue ( "Dest5Name" ), 28, UI.UserInterfaceHelper.eCrestronFont.Arial, UI.UserInterfaceHelper.eNamedColour.Silver );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestSelectList ].StringInput[ 21 ].StringValue = UI.UserInterfaceHelper.FormatTextForUi ( cdsInt.GetLocalSerialValue ( "Dest6Name" ), 28, UI.UserInterfaceHelper.eCrestronFont.Arial, UI.UserInterfaceHelper.eNamedColour.Silver );

			uint i = 11;
			foreach (var route in VideoRoutes.routes)
			{
				string tag = $"Source{route.xioValue}Name";
				route.name = cdsInt.GetLocalSerialValue ( tag ); //set internal VideoRoutes.routes.name values from CDS
				_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceSelectionList ].StringInput[ i ].StringValue = UI.UserInterfaceHelper.FormatTextForUi ( route.name, 28, UI.UserInterfaceHelper.eCrestronFont.Arial, UI.UserInterfaceHelper.eNamedColour.Silver );
				i++;
			}

			//-----------------------------------------------------------------------------------------------------------------------------------------------

			//Set String Inputs to CDS Values, joins correspond to SRL joins---------------------------------------------------------------------------------
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringInput[ 12 ].StringValue = cdsInt.GetLocalSerialValue ( "Dest1Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringInput[ 14 ].StringValue = cdsInt.GetLocalSerialValue ( "Dest2Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringInput[ 16 ].StringValue = cdsInt.GetLocalSerialValue ( "Dest3Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringInput[ 18 ].StringValue = cdsInt.GetLocalSerialValue ( "Dest4Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringInput[ 20 ].StringValue = cdsInt.GetLocalSerialValue ( "Dest5Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestNameList ].StringInput[ 22 ].StringValue = cdsInt.GetLocalSerialValue ( "Dest6Name" );

			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringInput[ 12 ].StringValue = cdsInt.GetLocalSerialValue ( "Source1Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringInput[ 14 ].StringValue = cdsInt.GetLocalSerialValue ( "Source2Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringInput[ 16 ].StringValue = cdsInt.GetLocalSerialValue ( "Source3Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringInput[ 18 ].StringValue = cdsInt.GetLocalSerialValue ( "Source4Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringInput[ 20 ].StringValue = cdsInt.GetLocalSerialValue ( "Source5Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringInput[ 22 ].StringValue = cdsInt.GetLocalSerialValue ( "Source6Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringInput[ 24 ].StringValue = cdsInt.GetLocalSerialValue ( "Source7Name" );
			_xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.SourceNameList ].StringInput[ 26 ].StringValue = cdsInt.GetLocalSerialValue ( "Source8Name" );
			//-----------------------------------------------------------------------------------------------------------------------------------------------
		}
		
		private void NvxRxStreamChangeEvent ( Crestron.SimplSharpPro.DeviceSupport.Stream stream, StreamEventArgs args )
		{
			var nvx = stream.Owner as DmNvxBaseClass;
			string currentstream = nvx.Control.ServerUrlFeedback.StringValue;
			//ushort currentXioRoute = nvx.XioRouting.VideoOutFeedback.UShortValue;
			uint thisDest = nvx.ID;
			int destIndex = VideoRoutes.destinations.FindIndex(a => a.ipid == thisDest); //find the index within VideoRoutes.destination list


			if (Debug.nvxDebug) CrestronConsole.PrintLine ( $"NvxRxStreamChangeEvent\n" );
			if (Debug.nvxDebug) CrestronConsole.PrintLine ( $"currentStream: {currentstream}\n" );
			//CrestronConsole.PrintLine ( $"currentXioRoute: {currentXioRoute}\n" );
			if (Debug.nvxDebug) CrestronConsole.PrintLine ( $"ipid: {thisDest}\n" );
			if (Debug.nvxDebug) CrestronConsole.PrintLine ( $"Dest Index: {destIndex}\n" );

			//update destination list values
			VideoRoutes.destinations[destIndex].streamUrl = currentstream;
			//VideoRoutes.destinations[ destIndex ].currentXioRoute = currentXioRoute; //only use if nvx device is using Xio to route

			UpdateNvxRxInfo ( nvx );
		}

		private void NvxTxStreamChangeEvent(Crestron.SimplSharpPro.DeviceSupport.Stream stream, StreamEventArgs args)
        {
            //identification for debug
            if (Debug.nvxDebug) CrestronConsole.PrintLine($"NvxTxStreamChangeEvent");
			
            //get nvx object which owns the stream and cast it to baseclass
            var nvx = stream.Owner as DmNvxBaseClass;

			//get the ipid (decimal)
			var id = nvx.ID;

            //find index of that id in the VideoRoutes list containing the names, Xiovalues, streamURL
            var index = VideoRoutes.routes.FindIndex(i => i.ipid == id);

            //print to console for debugging
            if (Debug.nvxDebug) CrestronConsole.PrintLine($"StreamChange Event - index: {index} : {_nvxTxList[index].Control.ServerUrlFeedback.StringValue ?? "null"}");

            //Set the Stream URL for each transmitter in the VideoRoutes.routes list
            VideoRoutes.routes[index].streamUrl = _nvxTxList[index].Control.ServerUrlFeedback.StringValue ?? "null";

			UpdateNvxTxInfo ( nvx );
		}


		//*********Methods to deal with UI and Stream changes*************//

		private void UI_UpdatePage ()
		{
			CrestronConsole.PrintLine ( "Updating Menu" );

			//drive the pages high
			UI.UserInterfaceHelper.SetDigitalJoin ( _xpanel, (uint)PageJoins.Routing, _nav == 1 );
			UI.UserInterfaceHelper.SetDigitalJoin ( _xpanel, (uint)PageJoins.Audio, _nav == 2 );
			UI.UserInterfaceHelper.SetDigitalJoin ( _xpanel, (uint)PageJoins.Naming, _nav == 3 );
		}

		private void UpdateRoute (ushort dest)
		{
			DmNvxBaseClass nvxRx;
			var routeIndex = (_sourceSelection > 0) ? _sourceSelection - 1 : 0;

			SmartObject so = _xpanel.SmartObjects[ (uint)PanelSmartObjectIDs.DestSelectList ];

			VideoRoutes.destinations[ dest - 1 ].xioRoute = _sourceSelection;
			if (Debug.uiDebug) CrestronConsole.PrintLine ( $"changed dest route: VideoRoutes.destinations[{dest - 1}].currentXioRoute = {VideoRoutes.destinations[dest - 1].xioRoute}" );


			switch (dest) 
			{
				case 1:
					nvxRx = _lobbyRx;
					UI.SmartGraphicsHelper.SetSmartObjectTextByJoin ( so, 12, VideoRoutes.routes[ routeIndex ].name );
					UI.SmartGraphicsHelper.PulseSmartObjectDigitalJoinByJoin ( so, 4011, 250 );
					break;
				case 2:
					nvxRx = _engEastRx;
					UI.SmartGraphicsHelper.SetSmartObjectTextByJoin ( so, 14, VideoRoutes.routes[ routeIndex ].name );
					UI.SmartGraphicsHelper.PulseSmartObjectDigitalJoinByJoin ( so, 4015, 250 );
					break;
				case 3:
					nvxRx = _engWestRx;
					UI.SmartGraphicsHelper.SetSmartObjectTextByJoin ( so, 16, VideoRoutes.routes[ routeIndex ].name );
					UI.SmartGraphicsHelper.PulseSmartObjectDigitalJoinByJoin ( so, 4019, 250 );
					break;
				case 4:
					nvxRx = _engNorthRx;
					UI.SmartGraphicsHelper.SetSmartObjectTextByJoin ( so, 18, VideoRoutes.routes[ routeIndex ].name );
					UI.SmartGraphicsHelper.PulseSmartObjectDigitalJoinByJoin ( so, 4023, 250 );
					break;
				case 5:
					nvxRx = _serviceNorthRx;
					UI.SmartGraphicsHelper.SetSmartObjectTextByJoin ( so, 20, VideoRoutes.routes[ routeIndex ].name );
					UI.SmartGraphicsHelper.PulseSmartObjectDigitalJoinByJoin ( so, 4027, 250 );
					break;
				case 6:
					nvxRx = _serviceWestRx;
					UI.SmartGraphicsHelper.SetSmartObjectTextByJoin ( so, 22, VideoRoutes.routes[ routeIndex ].name );
					UI.SmartGraphicsHelper.PulseSmartObjectDigitalJoinByJoin ( so, 4031, 250 );
					break;
				default:
					nvxRx = null;
					break;
			}

			NvxRouteManager ( nvxRx, VideoRoutes.routes[routeIndex].streamUrl );
		}

		private void NvxRouteManager ( DmNvxBaseClass rx, string streamURL )
		{
			if (rx.Control.ServerUrlFeedback.StringValue != streamURL)
			{
				rx.Control.ServerUrl.StringValue = streamURL;
			}
		}

		public void UpdateActiveRouteIndicators ( BasicTriListWithSmartObject currentDevice, SmartObjectEventArgs args ) //with SmartObjectEventArgs
		{
			//SmartObject so = currentDevice.SmartObjects[args.SmartObjectArgs.ID];
			SmartObject so = currentDevice.SmartObjects[(uint)PanelSmartObjectIDs.DestSelectList];


			int j = 4012;
			foreach (var item in VideoRoutes.destinations)
			{
				if (Debug.uiDebug) CrestronConsole.PrintLine ( $"UpdateSourceLoop; Bool being evalutated = {j}, item index = {item.index}, cXio = {item.xioRoute}, " );
				CrestronConsole.PrintLine ( $"item.XioRoute-{item.xioRoute}, _sourceSelection-{_sourceSelection}, item.streamUrl-{item.streamUrl}, route.StreamUrl-{VideoRoutes.routes[_sourceSelection -1].streamUrl}" );
				UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, j, ((item.xioRoute == _sourceSelection) || (item.streamUrl == VideoRoutes.routes[ _sourceSelection -1 ].streamUrl)) );
				j += 4; //this SRL has 4 digitals per page ref
			}
		}

		public void UpdateActiveRouteIndicators ( BasicTriListWithSmartObject currentDevice ) //without SmartObjectEventArgs
		{
			SmartObject so = currentDevice.SmartObjects[(uint)PanelSmartObjectIDs.DestSelectList];

			int j = 4012;
			foreach (var item in VideoRoutes.destinations)
			{
				if (Debug.uiDebug) CrestronConsole.PrintLine ( $"UpdateSourceLoop; Bool being evalutated = {j}, item index = {item.index}, cXio = {item.xioRoute}, " );
				UI.SmartGraphicsHelper.SetSmartObjectDigitalJoinByJoin ( so, j, ((item.xioRoute == _sourceSelection) || (item.streamUrl == VideoRoutes.routes[ _sourceSelection -1 ].streamUrl)));
				j += 4; //this SRL has 4 digitals per page ref
			}
		}

		public void UpdateNvxTxInfo ( GenericBase currentDevice )
		{ 
			SmartObject so = _xpanel.SmartObjects[(uint)PanelSmartObjectIDs.NvxInfoList];
			
			var id = currentDevice.ID;
			var index = _nvxTxList.FindIndex(x => x.ID == id );
			var offset = 0;

			if (index >= 0)
			{
				switch (index)
				{
					case 0:
						offset = 11;
						break;
					case 1:
						offset = 15;
						break;
					case 2:
						offset = 19;
						break;
					case 3:
						offset = 23;
						break;
					case 4:
						offset = 27;
						break;
					case 5:
						offset = 31;
						break;
					case 6:
						offset = 35;
						break;
					case 7:
						offset = 39;
						break;
				}

				so.StringInput[ (ushort)(offset) ].StringValue = _nvxTxList[ index ].Description;
				so.StringInput[ (ushort)(offset + 1) ].StringValue = _nvxTxList[ index ].Control.ServerUrlFeedback.StringValue;
				so.StringInput[ (ushort)(offset + 2) ].StringValue = _nvxTxList[ index ].Network.HostNameFeedback.StringValue;
				so.StringInput[ (ushort)(offset + 3) ].StringValue = "NA";
			}
		}

		public void UpdateNvxRxInfo ( GenericBase currentDevice )
		{
			SmartObject so = _xpanel.SmartObjects[(uint)PanelSmartObjectIDs.NvxInfoList];

			var id = currentDevice.ID;
			
			var offset = 0;
			DmNvxBaseClass nvx = null;
			var index = 0;

			if (id >= 0)
			{
				switch (id)
				{
					case 0x21:
						offset = 43;
						nvx = _lobbyRx;
						index = 1;
						break;
					case 0x22:
						offset = 47;
						nvx = _engNorthRx;
						index = 2;
						break;
					case 0x23:
						offset = 51;
						nvx = _engEastRx;
						index = 3;
						break;
					case 0x24:
						offset = 55;
						nvx = _engWestRx;
						index = 4;
						break;
					case 0x25:
						offset = 59;
						nvx = _serviceNorthRx;
						index = 4;
						break;
					case 0x26:
						offset = 63;
						nvx = _serviceWestRx;
						index = 5;
						break;
				}

				so.StringInput[ (ushort)(offset) ].StringValue = nvx.Description;
				so.StringInput[ (ushort)(offset + 1) ].StringValue = nvx.Network.IpAddressFeedback.StringValue;
				so.StringInput[ (ushort)(offset + 2) ].StringValue = nvx.Network.HostNameFeedback.StringValue;

				//var currentSource = VideoRoutes.destinations[index].xioRoute;
				so.StringInput[ (ushort)(offset + 3) ].StringValue = VideoRoutes.destinations[index].streamUrl;
			}
		}


		//****************Event handlers for Online status and IP information changes*****************//
		public void XpanelIpInformationChange ( GenericBase currentDevice, ConnectedIpEventArgs args )
		{
			CrestronConsole.PrintLine ( $"Panel IP: {args.DeviceIpAddress}" );
		}

		public void XpanelOnlineStatusChange ( GenericBase currentDevice, OnlineOfflineEventArgs args )
		{
			CrestronConsole.PrintLine ( $"Panel {(args.DeviceOnLine ? "online" : "offline")}" );
			ErrorLog.Notice ( $"Panel {(args.DeviceOnLine ? "online" : "offline")}" );
		}

		private void NvxIpInformationChangeEvent ( GenericBase currentDevice, ConnectedIpEventArgs args )
		{
			if (Debug.nvxDebug) CrestronConsole.PrintLine ( $"{currentDevice.Description} ip address: {args.DeviceIpAddress}" );
			var device = currentDevice as DmNvxBaseClass;
		}

		private void NvxTxOnlineStatusChangeEvent ( GenericBase currentDevice, OnlineOfflineEventArgs args )
		{
			if (Debug._systemDebug) CrestronConsole.PrintLine ( $"{currentDevice.Name} '{currentDevice.Description}' @ {currentDevice.ID} is {(args.DeviceOnLine ? "online" : "offline")}" );

			
			if (args.DeviceOnLine)
			{
				UpdateNvxTxInfo ( currentDevice );
			}
		}

		private void NvxRxOnlineStatusChangeEvent ( GenericBase currentDevice, OnlineOfflineEventArgs args )
		{
			if (Debug._systemDebug) CrestronConsole.PrintLine ( $"{currentDevice.Name} '{currentDevice.Description}' @ {currentDevice.ID} is {(args.DeviceOnLine ? "online" : "offline")}" );


			if (args.DeviceOnLine)
			{
				UpdateNvxRxInfo ( currentDevice );
			}
		}
	}
}