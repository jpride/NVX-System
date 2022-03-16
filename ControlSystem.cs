using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using System.Threading;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DM.Streaming;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro.DM;

namespace NVX_System
{
    public class ControlSystem : CrestronControlSystem
    {
        private bool _debug = true;
        private  Tsw1070 _tsw1070;
        private  DmNvx350 _nvxRx;

        
        //List of nvx encoders
        private List<DmNvxE30> _nvxTxList = new List<DmNvxE30>();
             
  
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
                _tsw1070.BaseEvent += Tsw1070BaseEvent;
                _tsw1070.OnlineStatusChange += Tsw1070OnlineStatusChange;
                _tsw1070.IpInformationChange += Tsw1070IpInformationChange;
                _tsw1070.SigChange += Tsw1070SigChange; //this eventhandler is the most important
                

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

        private void SmartObjectSigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            if (_debug) CrestronConsole.PrintLine($"Smartobject used ID:{args.SmartObjectArgs.ID}  Signal {args.Sig.GetType()}");

            
            switch ((PanelSSmartObjectIDs)args.SmartObjectArgs.ID)
            {
                case PanelSSmartObjectIDs.StreamSelectionList:
                    {
                        if (_debug) CrestronConsole.PrintLine($"Stream Selection List button pressed; signal = {args.Sig.GetType()}, number = {args.Sig.Number}, name = {args.Sig.Name}");

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
                                    if (_debug) CrestronConsole.PrintLine($"Switching {_nvxRx.Description} to {VideoRoutes.routes[index].name} @ {VideoRoutes.routes[index].streamURL}");
                                    _nvxRx.Control.ServerUrl.StringValue = VideoRoutes.routes[index].streamURL;

                                    //Set the digital output high when this is selected
                                    _tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.StreamSelectionList].BooleanInput[(uint)index + 1].BoolValue = true;
                                }
                                else
                                {
                                    _tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.StreamSelectionList].BooleanInput[(uint)index + 1].BoolValue = false;
                                }
                            }
                        }
                        break;
                    }
                case PanelSSmartObjectIDs.NavList:
                    {
                        if (_debug) CrestronConsole.PrintLine($"Nav List button pressed; signal = {args.Sig.GetType()}, number = {args.Sig.Number}, name = {args.Sig.Name}");

                        if (Array.Exists(Joins.tswNavSmartObjectJoins, x => x == args.Sig.Number) && args.Sig.BoolValue)
                        {
                            foreach (var item in Joins.tswNavSmartObjectJoins)
                            {
                                if (args.Sig.Number == item)
                                {
                                    if (_debug) CrestronConsole.PrintLine($"Setting join {item + Joins.tswNavSmartObjFbOffset} high");
                                    _tsw1070.BooleanInput[item + Joins.tswNavSmartObjFbOffset].BoolValue = true;
                                }
                                else 
                                {
                                    if (_debug) CrestronConsole.PrintLine($"Setting join {item + Joins.tswNavSmartObjFbOffset} low");
                                    _tsw1070.BooleanInput[item + Joins.tswNavSmartObjFbOffset].BoolValue = false;
                                }
                            }
                        }
                        break;
                    }
                case PanelSSmartObjectIDs.SRList:
                    {
                        if (_debug) CrestronConsole.PrintLine($"SR List button pressed; signal = {args.Sig.GetType()}, number = {args.Sig.Number}, name = {args.Sig.Name}");

                        var srlSigWithoutOffset = args.Sig.Number - 4010; //*** srl Sig.Numbers start at 4010 for some reason ***
                        if (_debug) CrestronConsole.PrintLine($"srlSigWithoutOffset: {srlSigWithoutOffset}");

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
                                    if (_debug) CrestronConsole.PrintLine($"Switching {_nvxRx.Description} to {VideoRoutes.routes[index].name} @ {VideoRoutes.routes[index].streamURL}");
                                    _nvxRx.Control.ServerUrl.StringValue = VideoRoutes.routes[index].streamURL;

                                    //Set the digital output high when this is selected
                                    _tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.SRList].BooleanInput[fb_cue].BoolValue = true;
                                }
                                else
                                {
                                    _tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.SRList].BooleanInput[fb_cue].BoolValue = false;
                                }
                            }
                        }
                        break;
                    }

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
                    ConfigureDevice(_tsw1070);
                    ConfigureNVX(_nvxRx);

                    int i = 0;
                    foreach (var tx in _nvxTxList)
                    {
                        ConfigureNVX(tx);
                        i++;
                    }
                });

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

            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"ConfigureNVX() Error for {nvx.Description}: {e.Message}");
            }
        }

        //Event to update local Stream URL when the unit goes online and reports stream change
        private void NvxTxStreamChangeEvent(Crestron.SimplSharpPro.DeviceSupport.Stream stream, StreamEventArgs args)
        {
            //identification for debug
            //if (_debug) CrestronConsole.PrintLine($"NvxTxStreamChangeEvent");

            //get nvx object which owns the stream and cast it to your variant
            var nvx = (DmNvxE30)stream.Owner;

            //get the ipid (decimal)
            var id = nvx.ID;

            //find index of that id in the VideoRoutes list containing the names, Xiovalues, streamUR
            var index = VideoRoutes.routes.FindIndex(i => i.ipid == id);

            //print to console for debugging
            //if (_debug) CrestronConsole.PrintLine($"StreamChange Event - index: {index} : {_nvxTxList[index].Control.ServerUrlFeedback.StringValue ?? "null"}");

            //Set the Stream URL for each transmitter in the VideoRoutes.routes list
            VideoRoutes.routes[index].streamURL = _nvxTxList[index].Control.ServerUrlFeedback.StringValue ?? "null";

            
            foreach (var item in VideoRoutes.routes)
            {
                var text_cue = string.Format($"text-o{item.xioValue}");
                
                _tsw1070.StringInput[item.xioValue].StringValue = string.Format($"{item.streamURL}");

                try
                {
                    _tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.SRList].StringInput[item.xioValue].StringValue = string.Format($"{item.name}");
                    _tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.SRList].StringInput[text_cue].StringValue = string.Format($"{item.name}");
                }

                catch (Exception e)
                {
                    CrestronConsole.PrintLine($"NvxTxStreamChangeEvent Error setting SmartObject SRList StringInput[{item.xioValue}");
                    ErrorLog.Error($"Error setting SmartObject SRList StringInput[{item.xioValue}");
                }
               
            }

        }


        private void NvxIpInformationChangeEvent(GenericBase currentDevice, ConnectedIpEventArgs args)
        {
            CrestronConsole.PrintLine($"{currentDevice.Description} ip address: {args.DeviceIpAddress}");
        }

        private void NvxOnlineStatusChangeEvent(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {     
            if (_debug) CrestronConsole.PrintLine($"{currentDevice.Name} '{currentDevice.Description}' @ {currentDevice.ID} is {(args.DeviceOnLine ? "online" : "offline")}");
        }

        public void Tsw1070SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            
            //const int AnalogSliderGroupId = 1000;
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


        public void ConfigureDevice(BasicTriList device)
        {
            var analogSliderSigs = new List<UShortInputSig>();
            for (uint n = Joins.SliderJoins[0]; n <= Joins.SliderJoins[2]; n++)
            {
                device.UShortInput[n].TieInputToOutput(device.UShortOutput[n]);
                analogSliderSigs.Add(device.UShortInput[n]);
            }
            CreateSigGroup(Joins.AnalogSliderGroupId, analogSliderSigs.ToArray());


            foreach (var item in Joins.StreamSelectionSmartObjJoins)
            {
                //interate thru StreamSelectionBtns array and get index of each item
                var index = Array.IndexOf(Joins.StreamSelectionSmartObjJoins, item);

                //use that index to populate the serial 'input' (output to panel in simpl) with the stream name from the VideoRoutes.routes list
                _tsw1070.StringInput[Joins.StreamSelectionSmartObjJoins[index]].StringValue = String.Format($"<FONT size=\"40\">{VideoRoutes.routes[index].name}</FONT>");

                //populate the smartgraphics stringInput (outputs)....note that the StringInput index is 1 based!!!
                _tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.StreamSelectionList].StringInput[(uint)index+1].StringValue = String.Format($"<FONT size=\"40\">{VideoRoutes.routes[index].name}</FONT>");

                try
                {
                    var text_cue = string.Format($"text-o{index + 1}");
                    _tsw1070.SmartObjects[(uint)PanelSSmartObjectIDs.SRList].StringInput[text_cue].StringValue = String.Format($"<FONT size=\"40\">{VideoRoutes.routes[index].name}</FONT>");
                }

                catch (Exception e)
                {
                    CrestronConsole.PrintLine($"ConfigureDevice Error setting SmartObject SRList StringInput[{index + 1}");
                    ErrorLog.Error($"ConfigureDevice Error setting SmartObject SRList StringInput[{index + 1}");
                }
            }
        }

        public uint GetTimeToRamp(TimeSpan timeSpan)
        {
            return (uint)(timeSpan.TotalMilliseconds / 10);
        }

        public enum PanelSSmartObjectIDs
        { 
            StreamSelectionList = 1,
            NavList = 2,
            SRList = 3
        };


        public void Tsw1070IpInformationChange(GenericBase currentDevice, ConnectedIpEventArgs args)
        {
            CrestronConsole.PrintLine($"Panel IP: {args.DeviceIpAddress}");
        }

        public void Tsw1070OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            CrestronConsole.PrintLine($"Panel {(args.DeviceOnLine ? "online" : "offline")}");
            ErrorLog.Notice($"Panel {(args.DeviceOnLine ? "online" : "offline")}");
        }

        public void Tsw1070BaseEvent(GenericBase device, BaseEventArgs args)
        {
            //throw new NotImplementedException();
        }

    }
}