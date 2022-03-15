using Crestron.SimplSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVX_System
{
    public static class VideoRoutes
    {
        public class Route
        {
            public ushort xioValue;
            public string name;
            public string streamURI;
            public uint ipid;
        };

        public static List<Route> routes = new List<Route> 
        {
            new Route() {xioValue = 1, name = "Brightsign", streamURI = "", ipid = 11 },
            new Route() {xioValue = 2, name = "Solstice", streamURI = "", ipid = 12 },
            new Route() {xioValue = 3, name = "Unused", streamURI = "", ipid = 13 },
            new Route() {xioValue = 4, name = "Unused", streamURI = "", ipid = 14 },
            new Route() {xioValue = 5, name = "Unused", streamURI = "", ipid = 15 },
            new Route() {xioValue = 6, name = "Unused", streamURI = "", ipid = 16 },
            new Route() {xioValue = 7, name = "Lobby Sign", streamURI = "", ipid = 17 },
            new Route() {xioValue = 8, name = "Aux Sign", streamURI = "", ipid = 18 },
        };

        public static void ListRoutes()
        {
            //utility data dump here
            CrestronConsole.PrintLine($"VideoRoutes Dump");
            foreach (var item in VideoRoutes.routes)
            {
                CrestronConsole.PrintLine($"{item.xioValue} | {item.name} | {item.streamURI}");
            }
        }
        
      
        public static Dictionary<int, string> _XioSubList = new Dictionary<int, string>
        {
            { 0, "rtsp://10.14.1.127:554/live.sdp"},
            { 1, "rtsp://10.14.1.116:554/live.sdp"},
            { 2, "rtsp://10.14.1.166:554/live.sdp"},
            { 3, "rtsp://10.14.1.200:554/live.sdp"},
            { 4, "rtsp://10.14.1.243:554/live.sdp"},
            { 5, "rtsp://10.14.1.181:554/live.sdp"},
            { 6, "rtsp://10.14.1.138:554/live.sdp"},
            { 7, "rtsp://10.14.1.215:554/live.sdp"}
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
    }
}
