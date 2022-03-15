using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using System.Threading;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using System.IO;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DM.Streaming;
using Crestron.SimplSharpPro.DM;

namespace NVX_System
{
    public class ControlSystem : CrestronControlSystem
    {
        
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


                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += ControllerSystemEventHandler;
                CrestronEnvironment.ProgramStatusEventHandler += ControllerProgramEventHandler;
                CrestronEnvironment.EthernetEventHandler += ControllerEthernetEventHandler;

                
                //Assign event handlers
                //panels
                _tsw1070.Register();
                _tsw1070.BaseEvent += Tsw1070BaseEvent;
                _tsw1070.OnlineStatusChange += Tsw1070OnlineStatusChange;
                _tsw1070.IpInformationChange += Tsw1070IpInformationChange;
                _tsw1070.SigChange += Tsw1070SigChange; //this eventhandler is the most important
                _tsw1070.ButtonStateChange += Tsw1070ButtonStateChange;

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
            CrestronConsole.PrintLine($"NvxTxStreamChangeEvent");

            //get nvx object which owns the stream and cast it to your variant
            var nvx = (DmNvxE30)stream.Owner;

            //get the ipid (decimal)
            var id = nvx.ID;

            //find index of that id in the VideoRoutes list containing the names, Xiovalues, streamUR
            var index = VideoRoutes.routes.FindIndex(i => i.ipid == id);

            //print to console for debugging
            CrestronConsole.PrintLine($"StreamChange Event - index: {index} : {_nvxTxList[index].Control.ServerUrlFeedback.StringValue ?? "null"}");

            //Set the Stream URL for each transmitter in the VideoRoutes.routes list
            VideoRoutes.routes[index].streamURL = _nvxTxList[index].Control.ServerUrlFeedback.StringValue ?? "null";
            
        }


        private void NvxIpInformationChangeEvent(GenericBase currentDevice, ConnectedIpEventArgs args)
        {
            CrestronConsole.PrintLine($"{currentDevice.Description} ip address: {args.DeviceIpAddress}");
        }

        private void NvxOnlineStatusChangeEvent(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {     
            CrestronConsole.PrintLine($"{currentDevice.Name} '{currentDevice.Description}' @ {currentDevice.ID} is {(args.DeviceOnLine ? "online" : "offline")}");
        }


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






        public void Tsw1070SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            
            const int AnalogSliderGroupId = 1000;
            //the SigHelper CheckSignalPoperties Consoles out helpful info about the incoming signals
            //SigHelper.CheckSigProperties(args.Sig);

            if (args.Sig.Type == eSigType.Bool && args.Sig.Number == Joins.RampBtnJoin)
            {
                if (args.Sig.Number == Joins.RampBtnJoin && args.Sig.BoolValue)
                {
                    SigGroups[AnalogSliderGroupId].CreateRamp(finalRampValue: 65535, GetTimeToRamp(TimeSpan.FromSeconds(value: 3)));

                    //utility data dump here
                    VideoRoutes.ListRoutes();
                }

                

                
            }
            
            if (Array.Exists(Joins.StreamSelectionJoins, x => x == args.Sig.Number) && args.Sig.BoolValue)
            {
           

                foreach (var item in Joins.StreamSelectionJoins)
                {
                    if (args.Sig.Number == item)
                    {
                        var index = Array.IndexOf(Joins.StreamSelectionJoins, item);

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
                    SigGroups[AnalogSliderGroupId].UShortValue = value;
                }
            }
        }

        public void Tsw1070ButtonStateChange(GenericBase device, ButtonEventArgs args)
        {
            throw new NotImplementedException();
        }

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

        public void Tsw1070ExtenderHardButtonReservedSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
        {
            throw new NotImplementedException();
        }

        public void ConfigureDevice(BasicTriList device)
        {
            var analogSliderSigs = new List<UShortInputSig>();
            for (uint n = Joins.RampSliderJoins[0]; n <= Joins.RampSliderJoins[2]; n++)
            {
                device.UShortInput[n].TieInputToOutput(device.UShortOutput[n]);
                analogSliderSigs.Add(device.UShortInput[n]);
            }
            CreateSigGroup(Joins.AnalogSliderGroupId, analogSliderSigs.ToArray());


            foreach (var item in Joins.StreamSelectionJoins)
            {
                var index = Array.IndexOf(Joins.StreamSelectionJoins, item);
                _tsw1070.StringInput[Joins.StreamSelectionJoins[index]].StringValue = String.Format($"<FONT size=\"40\">{VideoRoutes.routes[index].name}</FONT>");
            }
        }

        public uint GetTimeToRamp(TimeSpan timeSpan)
        {
            return (uint)(timeSpan.TotalMilliseconds / 10);
        }

       

        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down. 
        /// Use these events to close / re-open sockets, etc. 
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values 
        /// such as whether it's a Link Up or Link Down event. It will also indicate 
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        private void ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType"></param>
        private void ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType"></param>
        private void ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }
    }
}