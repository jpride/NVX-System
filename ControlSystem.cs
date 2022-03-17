using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM.Streaming;
using Crestron.SimplSharpPro.UI;
using System;
using System.Collections.Generic;
using System.Threading;


namespace NVX_System
{
	public class ControlSystem : CrestronControlSystem
    {

		private bool _systemDebug = true;
		private bool _nvxDebug;
		private bool _uiDebug;

		public bool nvxDebug
			{
			get { return _nvxDebug; }
			set { _nvxDebug = value; }
			}

		public bool uiDebug
			{
			get { return _uiDebug; }
			set { _uiDebug = value; }
			}

		//devices
		private readonly  Tsw1070 _tsw1070;
        private readonly DmNvx350 _nvxRx;

        //List of nvx encoders
        private List<DmNvxE30> _nvxTxList = new List<DmNvxE30>();

		private ushort _sourceSelection; //1 based index of current source selection

        //Enum for SmartObjects
        public enum PanelSmartObjectIDs
        {
            StreamSelectionList = 1,
            NavList = 2,
            StreamSrl = 3
        };
		private ushort _nav;


		public ControlSystem() : base()
        {
            try
            {
                Crestron.SimplSharpPro.CrestronThread.Thread.MaxNumberOfUserThreads = 20;

				
				_tsw1070 = new Tsw1070(0x09, this) { Description = "Main Touchpanel" };
                _nvxRx = new DmNvx350(0x0A, this) { Description = "NVX Rx" };


                //loop to add encoders to the encoder list
                int j = 0;
                for (uint i = 0x0B; i <= 0x12; i++)
                {
                    _nvxTxList.Add(new DmNvxE30(i, this) { Description = $"Nvx-Tx-{j}" });
                    j++;
                }


                try
                {
                    string SDGFilePath = Path.Combine(Directory.GetApplicationDirectory(), "tsw1070.sgd");

                    if (File.Exists(SDGFilePath))
                    {
                        _tsw1070.LoadSmartObjects(SDGFilePath);

                        foreach (KeyValuePair<uint, SmartObject> pair in _tsw1070.SmartObjects)
                        {
                            pair.Value.SigChange += SmartObjectSigChange;
                        }

                        ErrorLog.Notice($"SGD File loaded!");
                    }
                    else
                    {
                        ErrorLog.Error("SmartGraphics Definition file not found! Set .sgd file to 'Copy Always'!");
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Error("Error loading smartgraphics definition");
                }


				
                //Assign event handlers
                //panels
                _tsw1070.Register();
                _tsw1070.OnlineStatusChange += Tsw1070OnlineStatusChange;
                _tsw1070.IpInformationChange += Tsw1070IpInformationChange;
                _tsw1070.SigChange += Tsw1070SigChange; //this eventhandler handles ALL panel signal events
				_tsw1070.SmartObjects[(uint)PanelSmartObjectIDs.NavList].SigChange += TSw1070NavChange; //this handles only events coming from SmartObject ID 2 (PanelSmartObjectIDs.NavList)
				_tsw1070.SmartObjects[(uint)PanelSmartObjectIDs.StreamSrl].SigChange += Tsw1070StreamSrlChange;
                

                //NVX
                //receivers
                _nvxRx.Register();
                

                //transmitters
                //loop through encoder list elements and register each
                foreach (var tx in _nvxTxList)
                {
                    tx.Register();
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in the control system constructor: {e.Message} || {e.InnerException}");
                throw;
            }
        }

		private void Tsw1070StreamSrlChange ( GenericBase currentDevice, SmartObjectEventArgs args )
		{
			if (_uiDebug) CrestronConsole.PrintLine ( $"Stream SRL button pressed; +" +
				$"signalType = {args.Sig.GetType ()}, " +
				$"number = {args.Sig.Number}, " +
				$"name = {args.Sig.Name}, " +
				$"UshortValue = {args.Sig.UShortValue}");

			if (args.Sig.Name == "Item Clicked")
			{
				_sourceSelection = args.Sig.UShortValue;
				tsw1070_UpdateStreaAndUI ();
			}
			
		}

		

		private void TSw1070NavChange ( GenericBase currentDevice, SmartObjectEventArgs args )
		{
			if (args.Sig.Name == "Item Clicked")
			{
				_nav = args.Sig.UShortValue; //for a h/v list, this value represents the analog value of the selected button with a 1-based index
				tsw1070_UpdateMenu ();
			}
		}

		//initialize system
		public override void InitializeSystem()
        {
            try
            {
                var programThread = new Thread(() =>
                {
                    //Program Entrypoint
                    ConfigureTSW(_tsw1070);
                    ConfigureNVX(_nvxRx);

                    int i = 0;
                    foreach (var tx in _nvxTxList)
                    {
                        ConfigureNVX(tx);
                        i++;
                    }

					CrestronConsole.AddNewConsoleCommand (
						SetNvxDebug,
						"setnvxdebug",
						"Sets nvx debug on control system",
						ConsoleAccessLevelEnum.AccessOperator );

					CrestronConsole.AddNewConsoleCommand (
						SetUiDebug,
						"setuidebug",
						"Sets ui debug on control system",
						ConsoleAccessLevelEnum.AccessOperator );
				} );

                programThread.Start();

            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in InitializeSystem: {e.Message}");
            }
        }



        public void ConfigureNVX(DmNvxBaseClass nvx)
        {
            try
            {
                //register events and their handlers and set AutoInitiation and Stream start
                nvx.OnlineStatusChange += NvxOnlineStatusChangeEvent;
                nvx.IpInformationChange += NvxIpInformationChangeEvent;
                nvx.Control.EnableAutomaticInitiation();
                nvx.Control.Start();
                nvx.SourceTransmit.StreamChange += NvxTxStreamChangeEvent;
				nvx.SourceReceive.StreamChange += NvxRxStreamChangeEvent;
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"ConfigureNVX() Error for {nvx.Description}: {e.Message}");
            }
        }

		public void ConfigureTSW ( BasicTriList device )
		{
			var analogSliderSigs = new List<UShortInputSig>();
			for (uint n = Joins.SliderJoins[ 0 ]; n <= Joins.SliderJoins[ 2 ]; n++)
			{
				device.UShortInput[ n ].TieInputToOutput ( device.UShortOutput[ n ] );
				analogSliderSigs.Add ( device.UShortInput[ n ] );
			}
			CreateSigGroup ( Joins.AnalogSliderGroupId, analogSliderSigs.ToArray ( ) );

			foreach (var item in Joins.StreamSelectionSmartObjJoins)
			{
				//interate thru StreamSelectionBtns array and get index of each item
				var index = Array.IndexOf(Joins.StreamSelectionSmartObjJoins, item);

				//use that index to populate the serial 'input' (output to panel in simpl) with the stream name from the VideoRoutes.routes list
				_tsw1070.StringInput[ Joins.StreamSelectionSmartObjJoins[ index ] ].StringValue = String.Format ( $"<FONT size=\"40\">{VideoRoutes.routes[ index ].name}</FONT>" );

				//populate the smartgraphics stringInput (outputs)....note that the StringInput index is 1 based!!!
				_tsw1070.SmartObjects[ (uint)PanelSmartObjectIDs.StreamSelectionList ].StringInput[ (uint)index + 1 ].StringValue = String.Format ( $"<FONT size=\"40\">{VideoRoutes.routes[ index ].name}</FONT>" );

				try
				{
					var text_cue = string.Format($"text-o{index + 1}");
					_tsw1070.SmartObjects[ (uint)PanelSmartObjectIDs.StreamSrl ].StringInput[ text_cue ].StringValue = String.Format ( $"<FONT size=\"40\">{VideoRoutes.routes[ index ].name}</FONT>" );
				}

				catch (Exception e)
				{
					CrestronConsole.PrintLine ( $"ConfigureDevice Error setting SmartObject SRList StringInput[{index + 1}" );
					ErrorLog.Error ( $"ConfigureDevice Error setting SmartObject SRList StringInput[{index + 1}" );
				}
			}
		}

		/// <summary>
		/// this event handler will capture a stream change on a Rx
		/// then it will check if the new stream is in the videoroutes list
		/// if it is, it will then update the UI
		/// This will only ever happen if the stream URL is changed outside of this program, manually. 
		/// so its not super useful, but is a good proof of concept.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="args"></param>
		private void NvxRxStreamChangeEvent ( Crestron.SimplSharpPro.DeviceSupport.Stream stream, StreamEventArgs args )
		{
			var nvx = stream.Owner as DmNvx350;

			string currentstream = nvx.Control.ServerUrlFeedback.StringValue;
			ushort currentXioRoute = nvx.XioRouting.VideoOutFeedback.UShortValue;

			if (_nvxDebug) CrestronConsole.PrintLine ( $"Xio Routing: {nvx.XioRouting.VideoOutFeedback.UShortValue}" );

			foreach (var route in VideoRoutes.routes)
			{
				if (currentstream == route.streamURL || currentXioRoute == route.xioValue)
				{
					_sourceSelection = route.xioValue;
					tsw1070_UpdateStreaAndUI ( );
					break;
				}
			}
		}

		/// <summary>
		/// Event to update local Stream URL when the unit goes online and reports stream change
		/// </summary>
		private void NvxTxStreamChangeEvent(Crestron.SimplSharpPro.DeviceSupport.Stream stream, StreamEventArgs args)
        {
            //identification for debug
            if (_nvxDebug) CrestronConsole.PrintLine($"NvxTxStreamChangeEvent");

            //get nvx object which owns the stream and cast it to your variant
            var nvx = stream.Owner as DmNvxE30;

            //get the ipid (decimal)
            var id = nvx.ID;

            //find index of that id in the VideoRoutes list containing the names, Xiovalues, streamUR
            var index = VideoRoutes.routes.FindIndex(i => i.ipid == id);

            //print to console for debugging
            if (_nvxDebug) CrestronConsole.PrintLine($"StreamChange Event - index: {index} : {_nvxTxList[index].Control.ServerUrlFeedback.StringValue ?? "null"}");

            //Set the Stream URL for each transmitter in the VideoRoutes.routes list
            VideoRoutes.routes[index].streamURL = _nvxTxList[index].Control.ServerUrlFeedback.StringValue ?? "null";
            
            foreach (var item in VideoRoutes.routes)
            {
                var text_cue = string.Format($"text-o{item.xioValue}");
                
                _tsw1070.StringInput[item.xioValue].StringValue = string.Format($"{item.streamURL}");

                try
                {
                    //_tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.SRList].StringInput[item.xioValue].StringValue = string.Format($"{item.name}");
                    _tsw1070.SmartObjects[(uint)PanelSmartObjectIDs.StreamSrl].StringInput[text_cue].StringValue = string.Format($"{item.name}");
                }

                catch (Exception e)
                {
                    CrestronConsole.PrintLine($"NvxTxStreamChangeEvent Error setting SmartObject SRList StringInput[{item.xioValue}");
                    ErrorLog.Error($"Error setting SmartObject SRList StringInput[{item.xioValue}");
                }
               
            }

        }



        public void Tsw1070SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            //**************************************************************
            //TSW_UI is a static class for handling SigEvents from various events
            //TSW_UI.ProcessSigChange(currentDevice, args);   
            //**************************************************************
            
			
            //the SigHelper CheckSignalPoperties Consoles out helpful info about the incoming signals
            //SigHelper.CheckSigProperties(args.Sig);

            if (args.Sig.Type == eSigType.Bool && args.Sig.Number == Joins.RampGroupBtn)
            {
                if (args.Sig.Number == Joins.RampGroupBtn && args.Sig.BoolValue)
                {
                    SigGroups[Joins.AnalogSliderGroupId].CreateRamp(finalRampValue: 65535, GetTimeToRamp(TimeSpan.FromSeconds(value: 3)));

                    //utility data dump here
                    VideoRoutes.ListRoutes();
                }



				
            }

			
            
            //if we find the sig number (join) in the Joins.StreamSelectionBtns joins list AND the value is true (rising edge)
            if (Array.Exists(Joins.StreamSelectionSmartObjJoins, x => x == args.Sig.Number) && args.Sig.BoolValue)
            {
           
                //loops through the join numbers in the array
                foreach (var item in Joins.StreamSelectionSmartObjJoins)
                {
                    if (args.Sig.Number == item)
                    {
                        //equate the join number to the index it has in the list
                        var index = Array.IndexOf(Joins.StreamSelectionSmartObjJoins, item);

                       //***routing via StreamLocation URL settings***
                        CrestronConsole.PrintLine($"Switching {_nvxRx.Description} to {VideoRoutes.routes[index].name} @ {VideoRoutes.routes[index].streamURL}");
                        _nvxRx.Control.ServerUrl.StringValue = VideoRoutes.routes[index].streamURL;

                        //OR
                        //
                        //***routing via XioSubscriptions***
                        //_rx01.XioRouting.VideoOut.UShortValue = VideoRoutes.XioSubscriptions[index];
                        //_rx01.XioRouting.VideoOut.UShortValue = VideoRoutes.routes[index].xioValue;

                        //Set the digital output high when this is selected
                        _tsw1070.BooleanInput[item].BoolValue = true;
                    }
                    else
                    {
                        _tsw1070.BooleanInput[item].BoolValue = false;
                    }
                }
            }

            else if (args.Sig.Type == eSigType.String && args.Sig.Number == Joins.SerialInputRampJoins)
            {
                if (ushort.TryParse(args.Sig.StringValue, out ushort value))
                {
                    SigGroups[Joins.AnalogSliderGroupId].UShortValue = value;
                }
            }
        }

        private void SmartObjectSigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            //if (_uiDebug) CrestronConsole.PrintLine($"Smartobject used ID:{args.SmartObjectArgs.ID}  Signal {args.Sig.GetType()}");


            switch ((PanelSmartObjectIDs)args.SmartObjectArgs.ID)
            {
				/*
                case PanelSmartObjectIDs.StreamSelectionList: //Vertical Button List (no digital feedback available) for source selection
                    {
                        if (_uiDebug) CrestronConsole.PrintLine($"Stream Selection List button pressed; signal = {args.Sig.GetType()}, number = {args.Sig.Number}, name = {args.Sig.Name}");

                        //if we find the sig number (join) in the Joins.StreamSelectionBtns joins list AND the value is true (rising edge)
                        if (Array.Exists(Joins.StreamSelectionSmartObjJoins, x => x == args.Sig.Number))
                        {

                            //loops through the join numbers in the array
                            foreach (var item in Joins.StreamSelectionSmartObjJoins)
                            {
                                //equate the join number to the index it has in the list
                                var index = Array.IndexOf(Joins.StreamSelectionSmartObjJoins, item);

                                if (args.Sig.Number == item && args.Sig.BoolValue)
                                {
                                    //***routing via StreamLocation URL settings***
                                    if (_uiDebug) CrestronConsole.PrintLine($"Switching {_nvxRx.Description} to {VideoRoutes.routes[index].name} @ {VideoRoutes.routes[index].streamURL}");
                                    _nvxRx.Control.ServerUrl.StringValue = VideoRoutes.routes[index].streamURL;
                                }
                            }
                        }
                        break;
                    }
				*/
                /*
                 case PanelSSmartObjectIDs.NavList: //Horizontal Button List for page nav
                    {
                        if (_uiDebug) CrestronConsole.PrintLine($"Nav List button pressed; signal = {args.Sig.GetType()}, number = {args.Sig.Number}, name = {args.Sig.Name}");

						if (args.Sig.Name == "Item Clicked")
						{
							_nav = args.Sig.UShortValue; //for a h/v list, this value represents the analog value of the selected button with a 1-based index
							tsw1070_UpdateMenu ();
						}
                        break;
                    }*/
				/*
                case PanelSmartObjectIDs.StreamSrl: //Subpage Reference List for source selection
                    {
                        if (_uiDebug) CrestronConsole.PrintLine($"SR List button pressed; signal = {args.Sig.GetType()}, number = {args.Sig.Number}, name = {args.Sig.Name}");

                        var srlSigWithoutOffset = args.Sig.Number - 4010; //*** srl Sig.Numbers start at 4010 for some reason ***
                        if (_uiDebug) CrestronConsole.PrintLine($"srlSigWithoutOffset: {srlSigWithoutOffset}");

                        //if we find the sig number (join) in the Joins.StreamSelectionBtns joins list AND the value is true (rising edge)
                        if (Array.Exists(Joins.StreamSelectionSmartObjJoins, x => x == srlSigWithoutOffset) && args.Sig.BoolValue)
                        {

                            //loops through the join numbers in the array
                            foreach (var item in Joins.StreamSelectionSmartObjJoins)
                            {
                                //equate the join number to the index it has in the list
                                var index = Array.IndexOf(Joins.StreamSelectionSmartObjJoins, item);
                                var fb_cue = string.Format($"fb{index + 1}");

                                if (srlSigWithoutOffset == item)
                                {

                                    //***routing via StreamLocation URL settings***
                                    if (_uiDebug) CrestronConsole.PrintLine($"Switching {_nvxRx.Description} to {VideoRoutes.routes[index].name} @ {VideoRoutes.routes[index].streamURL}");
                                    _nvxRx.Control.ServerUrl.StringValue = VideoRoutes.routes[index].streamURL;

                                    //Set the digital output high when this is selected
                                    _tsw1070.SmartObjects[(uint)PanelSmartObjectIDs.StreamSrl].BooleanInput[fb_cue].BoolValue = true;
                                }
                                else
                                {
                                    _tsw1070.SmartObjects[(uint)PanelSmartObjectIDs.StreamSrl].BooleanInput[fb_cue].BoolValue = false;
                                }
                            }
                        }
                        break;
                    }
				*/

            }

        }

		private void tsw1070_UpdateMenu ()
		{
			//two ways to do this.
			//Use an nav join array to loop thru items and set page fb high for matches and low otherwise
			
			//foreach (var item in Joins.tswNavSmartObjectJoins)
			//{
			//	//Joins.tswNavSmartObjectJoins[i] + Joins.tswNavSmartObjFbOffset = [1,2,3] + 20 (20 is the offset to the page feedback)
			//	//(_nav == Joins.tswNavSmartObjectJoins[i]) resolves to 'true' when the _nav variable is equal to the item (which are the values of the Joins.tswNavSmartObjectJoins array and 'false' when not
			//	if (_uiDebug) CrestronConsole.PrintLine ( $"Setting join {item + Joins.tswNavSmartObjFbOffset} {_nav == item}" );
			//	_tsw1070.BooleanInput[item + Joins.tswNavSmartObjFbOffset].BoolValue = (_nav == item);
			//}

			//OR

			//explicitly define page fb and nav button joins with comments for clarity
			//this is useful because if your navigation is done using an SGO, then you wont have to create an array as simple as [1,2,3] 
			_tsw1070.BooleanInput[21].BoolValue = (_nav == 1); //Audio Control
			_tsw1070.BooleanInput[22].BoolValue = (_nav == 2); //NVX Control
		}

		private void tsw1070_UpdateStreaAndUI ()
		{
			var routeIndex = _sourceSelection - 1;


			if (_uiDebug) CrestronConsole.PrintLine ( $"Switching {_nvxRx.Description} to {VideoRoutes.routes[routeIndex].name} @ {VideoRoutes.routes[routeIndex].streamURL}" );
			NvxRouteManager ( _nvxRx, VideoRoutes.routes[routeIndex].streamURL );


			//Set the digital output high when this is selected
			string fb_cue;
			for (int i = 0; i < VideoRoutes.routes.Count; i++)
			{
				fb_cue = string.Format ( $"fb{i + 1}" );
				_tsw1070.SmartObjects[(uint)PanelSmartObjectIDs.StreamSrl].BooleanInput[fb_cue].BoolValue = (_sourceSelection == VideoRoutes.routes[i].xioValue);
			}
		}

		private void NvxRouteManager ( DmNvxBaseClass rx, string streamURL )
		{
			if (rx.Control.ServerUrlFeedback.StringValue != streamURL)
			{
				rx.Control.ServerUrl.StringValue = streamURL;
			}
		}

        public uint GetTimeToRamp(TimeSpan timeSpan)
        {
            return (uint)(timeSpan.TotalMilliseconds / 10);
        }

		private void SetNvxDebug ( string parms )
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
				CrestronConsole.ConsoleCommandResponse ( $"nvxDebug set to {nvxDebug}" );
			}
		}

		private void SetUiDebug ( string parms )
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
				CrestronConsole.ConsoleCommandResponse ( $"uiDebug set to {uiDebug}" );
			}
		}







		public void Tsw1070IpInformationChange ( GenericBase currentDevice, ConnectedIpEventArgs args )
		{
			CrestronConsole.PrintLine ( $"Panel IP: {args.DeviceIpAddress}" );
		}

		public void Tsw1070OnlineStatusChange ( GenericBase currentDevice, OnlineOfflineEventArgs args )
		{
			CrestronConsole.PrintLine ( $"Panel {(args.DeviceOnLine ? "online" : "offline")}" );
			ErrorLog.Notice ( $"Panel {(args.DeviceOnLine ? "online" : "offline")}" );
		}


		private void NvxIpInformationChangeEvent ( GenericBase currentDevice, ConnectedIpEventArgs args )
		{
			CrestronConsole.PrintLine ( $"{currentDevice.Description} ip address: {args.DeviceIpAddress}" );
		}

		private void NvxOnlineStatusChangeEvent ( GenericBase currentDevice, OnlineOfflineEventArgs args )
		{
			if (_systemDebug) CrestronConsole.PrintLine ( $"{currentDevice.Name} '{currentDevice.Description}' @ {currentDevice.ID} is {(args.DeviceOnLine ? "online" : "offline")}" );
		}

	}
}