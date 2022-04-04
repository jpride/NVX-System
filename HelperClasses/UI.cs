using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using System.Threading;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using System.IO;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DM.Streaming;



namespace TSISignageApp
{
    //this is a test to understand how to pass a sigevent's device and args into a class method
    public static class TSW_UI
    {
        public static void ProcessSigChange(BasicTriList device, SigEventArgs args) 
        {
            CrestronConsole.PrintLine($"ProcessSigChange: Received {device} and args: {args}");

            device.StringInput[1].StringValue = "test";

        }
    
    }
}
