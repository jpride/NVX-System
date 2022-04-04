using Crestron.SimplSharpPro;
using Crestron.SimplSharp;
using System;

namespace TSISignageApp
{
    internal class SigHelper
    {
        public static void CheckSigProperties(Sig sig)
        {
            CrestronConsole.PrintLine($"\n\nSig Name: {sig.Name}");
            CrestronConsole.PrintLine($"Sig Number: {sig.Number}");
            //CrestronConsole.PrintLine($"Sig Supported: {sig.Supported}");
            //CrestronConsole.PrintLine($"Sig UserObject: {sig.UserObject ?? "<NULL>"}");

            var signalDirection = sig.IsInput ? "input" : "output";

            switch (sig.Type)
            {
                case eSigType.NA:
                    CrestronConsole.PrintLine("Sig Type is not supported");
                    break;
                case eSigType.Bool:
                    CrestronConsole.PrintLine($"Digital {signalDirection} signal: {sig.BoolValue}");
                    break;
                case eSigType.UShort:
                    CrestronConsole.PrintLine($"Analog {signalDirection} signal: {sig.UShortValue} (Signed: {sig.ShortValue})");
                    if (sig.IsRamping)
                    {
                        CrestronConsole.PrintLine($"Signal is ramping ({(sig.RampingInformation.IsSigned ? "signed" : "unsigned")})");
                        CrestronConsole.PrintLine($"BaseTimeForRamping: {sig.RampingInformation.BaseTimeForRamping}");
                        CrestronConsole.PrintLine($"Transition Time for ramping: {sig.RampingInformation.TransitionTimeForRamp}");

                        if (sig.RampingInformation.IsSigned)
                        {
                            CrestronConsole.PrintLine($"{sig.RampingInformation.BaseValueForSignedRamp}");
                            CrestronConsole.PrintLine($"{sig.RampingInformation.FinalValueForSignedRamp}");
                        }
                        else
                        {
                            CrestronConsole.PrintLine($"{sig.RampingInformation.BaseValueForRamp}");
                            CrestronConsole.PrintLine($"{sig.RampingInformation.FinalValueForRamp}");
                        }
                    }
                    break;
                case eSigType.String:
                    CrestronConsole.PrintLine($"Serial {signalDirection} signal: {sig.StringValue}");
                    if (sig.StringEncoding != eStringEncoding.eEncodingUnknown)
                    {
                        CrestronConsole.PrintLine($"Encoding:{(sig.StringEncoding == eStringEncoding.eEncodingASCII ? "ASCII" : "UTF16")}");
                    }
                    else 
                    {
                        CrestronConsole.PrintLine($"Encoding: <Unknown>");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sig), "Sig type is not valid for the enumeration");
            }

        }
    }
}