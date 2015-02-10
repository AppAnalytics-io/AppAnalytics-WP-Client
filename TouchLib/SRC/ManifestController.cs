using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TouchLib
{
    class ManifestController
    {
        private byte kDataPackageFileVersion = 1;
        private byte kSessionManifestFileVersion   = 1;
        private byte kAppAnalyticsApiVersion = 1;

        // [0-1]=DataPackageFileSignature:word; //'H'+'A'
        // [2]=DataPackageFileVersion:byte;
        // [3-38]= SessionId:UUIDV4; //UUID to keep every session unique.
        public String buildHeader()
        {
//             byte[] bytes = { (byte)'H', (byte)'A', kDataPackageFileVersion };
//             // - header -
//             byte[] sessionID = new byte[38-3];
//             sessionID = 
            StringBuilder byteStr = new StringBuilder("HA" + kDataPackageFileVersion.ToString());

            byteStr.Append(Detector.getSessionID());

            return byteStr.ToString();
        }

        // [0]=PackageBeginMarker:byte; //ASCII#60 '<'
        // [1-8]=ActionOrder:uint64; //Unique index value of the gesture action order.
        // [9]=ActionId:byte; //Predefined Unique event ID for every gesture action.
        // [10-17]=ActionTime:DateTime; //the date and time value when the action occured.
        // [18-25]=PositionX:double; //X coordinate value where the action begins.
        // [26-33]=PositionY:double; //Y coordinate value where the action begins.
        // [34-37]=Param1:variant; //Reserved: ScalePercent for zoom/pinch, RotationDegree for rotation, Shake Orientation for shakes.
        // [...]=ViewIdLength:UInt16; //total length of ViewId value.
        // [...]=ViewId:string; //String value of Page/view ID where the gesture action happens.
        // [...]=ElementIdLength:UInt16; //total length of ElementId value.
        // [...]=ElementId:string; //the ID of a user interface element related to the gesture action.
        // [Length-1]=PackageEndMarker:byte; //ASCII#62 '>'
        public String buildDataPackage(GestureData data)
        {
            StringBuilder byteStr = new StringBuilder("<");

            // - package -

            return byteStr.Append(">").ToString();
        }

        // [0]=PackageBeginMarker:byte; //ASCII#60 '<'
        // [1]= SessionManifestFileVersion:byte;
        // [2-37]= SessionId:UUIDV4; //UUID to keep every session unique.
        // [38-45]= SessionStartDate:uint64; //Holds when the session starts.
        // [46-53]= SessionEndDate:uint64; //holds when the session ends.
        // [54-85]=UDID:string; //Unique device ID.
        // [86-93]= ResolutionX:double; //the total width of the client screen.
        // [94-101]= ResolutionY:double; //the total height of the client screen.
        // [102]=APIVersion:byte;
        // [103-134]=APIKey:string;
        // [134-150]=APPVersion:Version; //the Major, Minor, Build and Revision version numbers client application as Unsigned Integers.
        // [151-166]=OSVersion:Version; //the Major, Minor, Build and Revision version numbers of client OS as Unsigned Integers.
        // [167-169]=SystemLocale:String; //the three-letter identifier for the region of the client.
        // [170]=PackageEndMarker:byte; //ASCII#62 '>'
        public String buildSessionManifest()
        {
            StringBuilder byteStr = new StringBuilder("<");

            byteStr.Append(kSessionManifestFileVersion);
            byteStr.Append(Detector.getSessionID());
            byteStr.Append(Detector.getSessionStartDate());
            byteStr.Append(Detector.getSessionEndDate());
            byteStr.Append(Detector.getUDID());
            byteStr.Append(Detector.getResolutionX());
            byteStr.Append(Detector.getResolutionY());
            byteStr.Append(Detector.ApiVersion);
            byteStr.Append(Detector.AppVersion);
            byteStr.Append(Detector.OSVersion);
            byteStr.Append(Detector.SystemLocale);

            return byteStr.Append(">").ToString();
        }
    }
}
