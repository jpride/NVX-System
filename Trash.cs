using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVX_System
{
    class Trash
    {
    }
}

/*
public override void InitializeSystem()
{
    try
    {
        var programThread = new Thread(() =>
        {
            //Program Entrypoint


            var tswRegistrationResult = _tsw1070.Register();
            if (tswRegistrationResult != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                var failureReason = _tsw1070.RegistrationFailureReason;

                ErrorLog.Error($"Failed to register {nameof(_tsw1070)}: {failureReason}");
            }

            if (_tsw1070.Registered)
            {
                CrestronConsole.PrintLine($"{nameof(_tsw1070)} registered successfully");

                const string smartObjectFilePath = @"\user\sgd\tsw760.sgd";
                if (File.Exists(smartObjectFilePath))
                {
                    var smartObjectsLoaded = _tsw1070.LoadSmartObjects(smartObjectFilePath);
                    CrestronConsole.PrintLine($"{nameof(_tsw1070)} loaded {smartObjectsLoaded} smart objects from {smartObjectFilePath}");
                }
                else
                {
                    ErrorLog.Error($"Smartgraphics file does not exist: {smartObjectFilePath}");
                }

                ConfigureDevice(_tsw1070);
            }


            var rxRegistrationResult = _rx01.Register();
            CrestronConsole.PrintLine($"TX Result: {rxRegistrationResult}");


            //var txRegistrationResult = _tx01.Register();
            //CrestronConsole.PrintLine($"TX Result: {txRegistrationResult}");

        });

        programThread.Start();
    }
    catch (Exception e)
    {
        CrestronConsole.PrintLine($"Error in InitializeSystem: {e.Message}");
    }
}
*/