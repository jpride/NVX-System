using Crestron.SimplSharp;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM.Streaming;

namespace TSISignageApp
{
    public static class VideoRoutes
    {
		/// <summary>
		/// This class will store info about the routes in our system
		/// xioValue represents the XioRouting index of the stream
		/// </summary>
		/// 

		public abstract class VideoEndpointObject //abstract because this class is only used as a base class
		{
			public ushort   index;
			public string   name;
			public uint     ipid;
			public string   streamUrl;
		};

        public class Route : VideoEndpointObject
        {
            public ushort	xioValue;
        };


		public class Destination : VideoEndpointObject
		{
			public ushort	xioRoute;
			public string   currentSrc;
		};

		public static List<Destination> destinations = new List<Destination>
		{ 
			new Destination() { index = 1, name = "Lobby",		ipid = 33},
			new Destination() { index = 2, name = "Office 1",	ipid = 34},
			new Destination() { index = 3, name = "Office 2",	ipid = 35},
			new Destination() { index = 4, name = "Operations", ipid = 36},
			new Destination() { index = 5, name = "Service 1",	ipid = 37},
			new Destination() { index = 6, name = "Service 2",	ipid = 38},
		};

        public static List<Route> routes = new List<Route> 
        {
            new Route() {index = 1,		xioValue = 1,	name = "Brightsign",		streamUrl = "", ipid = 11 },
            new Route() {index = 2,		xioValue = 2,	name = "Solstice",			streamUrl = "", ipid = 12 },
            new Route() {index = 3,		xioValue = 3,	name = "Unused",			streamUrl = "", ipid = 13 },
            new Route() {index = 4,		xioValue = 4,	name = "Unused",			streamUrl = "", ipid = 14 },
            new Route() {index = 5,		xioValue = 5,	name = "Unused",			streamUrl = "", ipid = 15 },
            new Route() {index = 6,		xioValue = 6,	name = "Unused",			streamUrl = "", ipid = 16 },
            new Route() {index = 7,		xioValue = 7,	name = "Lobby Sign",		streamUrl = "", ipid = 17 },
            new Route() {index = 8,		xioValue = 8,	name = "Aux Sign",			streamUrl = "", ipid = 18 },
        };

        
        //string List with StreamLocation values listed (ideally, this should be gleaned from the transmitters directly)
        public static List<string> StreamLocationList = new List<string> {
            "rtsp://10.14.1.127:554/live.sdp",
            "rtsp://10.14.1.116:554/live.sdp",
            "rtsp://10.14.1.166:554/live.sdp",
            "rtsp://10.14.1.200:554/live.sdp",
            "rtsp://10.14.1.243:554/live.sdp",
            "rtsp://10.14.1.181:554/live.sdp",
            "rtsp://10.14.1.138:554/live.sdp",
            "rtsp://10.14.1.215:554/live.sdp"
        };


        public static void ListRoutes()
        {
            //utility data dump here
            CrestronConsole.PrintLine($"VideoRoutes Dump");
            foreach (var item in VideoRoutes.routes)
            {
                CrestronConsole.PrintLine($"{item.xioValue} | {item.name} | {item.streamUrl}");
            }
        }
    }
}
