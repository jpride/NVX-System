using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using System.Threading;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using System.IO;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DM.Streaming;



namespace NVX_System
{
    /*
    public class UI
    {
        public void SigHandler(BasicTriList device, SigEventArgs args)
        {
            
            //the SigHelper CheckSignalPoperties Consoles out helpful info about the incoming signals
            //SigHelper.CheckSigProperties(args.Sig);

            if (args.Sig.Type == eSigType.Bool && args.Sig.Number == Joins.RampBtnJoin)
            {
                if (args.Sig.Number == Joins.RampBtnJoin && args.Sig.BoolValue)
                {
                    ControlSystem.SigGroups[Joins.AnalogSliderGroupId].CreateRamp(finalRampValue: 65535, GetTimeToRamp(TimeSpan.FromSeconds(value: 3)));
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
                        CrestronConsole.PrintLine($"Switching {ControlSystem._nvxRx.Description} to {VideoRoutes.routes[index].name} @ {VideoRoutes.routes[index].streamURI}");
                        _nvxRx.Control.ServerUrl.StringValue = VideoRoutes.routes[index].streamURI;

                        //OR
                        //
                        //***routing via XioSubscriptions***
                        //_rx01.XioRouting.VideoOut.UShortValue = VideoRoutes.XioSubscriptions[index];
                        //_rx01.XioRouting.VideoOut.UShortValue = VideoRoutes.routes[index].xioValue;

                        //Set the digital output high when this is selected
                        device.BooleanInput[item].BoolValue = true;
                    }
                    else
                    {
                        device.BooleanInput[item].BoolValue = false;
                    }
                }
            }

            else if (args.Sig.Type == eSigType.String && args.Sig.Number == Joins.SerialInputRampJoins)
            {
                if (ushort.TryParse(args.Sig.StringValue, out ushort value))
                {
                    ControlSystem.SigGroups[Joins.AnalogSliderGroupId].UShortValue = value;
                }
            }
        }

        public uint GetTimeToRamp(TimeSpan timeSpan)
        {
            return (uint)(timeSpan.TotalMilliseconds / 10);
        }

    }
    */
}
