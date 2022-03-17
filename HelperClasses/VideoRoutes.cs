using Crestron.SimplSharp;
using System.Collections.Generic;

namespace NVX_System
{
    public static class VideoRoutes
    {
		/// <summary>
		/// This class will store info about the routes in our system
		/// xioValue represents the XioRouting index of the stream
		/// </summary>
        public class Route
        {
            public ushort xioValue;
            public string name;
            public string streamURL;
            public uint ipid;
        };

        public static List<Route> routes = new List<Route> 
        {
            new Route() {xioValue = 1, name = "Brightsign", streamURL = "", ipid = 11 },
            new Route() {xioValue = 2, name = "Solstice", streamURL = "", ipid = 12 },
            new Route() {xioValue = 3, name = "Unused", streamURL = "", ipid = 13 },
            new Route() {xioValue = 4, name = "Unused", streamURL = "", ipid = 14 },
            new Route() {xioValue = 5, name = "Unused", streamURL = "", ipid = 15 },
            new Route() {xioValue = 6, name = "Unused", streamURL = "", ipid = 16 },
            new Route() {xioValue = 7, name = "Lobby Sign", streamURL = "", ipid = 17 },
            new Route() {xioValue = 8, name = "Aux Sign", streamURL = "", ipid = 18 },
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
                CrestronConsole.PrintLine($"{item.xioValue} | {item.name} | {item.streamURL}");
            }
        }
    }
}
