using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace TouchLib
{
    public enum GestureID
    {
        SingleTapWith1Finger = 1,
        DoubleTapWith1Finger = 2,
        TripleTapWith1Finger = 3,
        SingleTapWith2Finger = 4,
        DoubleTapWith2Finger = 5,
        TripleTapWith2Finger = 6,
        SingleTapWith3Finger = 7,
        DoubleTapWith3Finger = 8,
        TripleTapWith3Finger = 9,
        SingleTapWith4Finger = 10,
        DoubleTapWith4Finger = 11,
        TripleTapWith4Finger = 12,
        HoldWith1Finger = 13,
        HoldWith2Finger = 14,
        HoldWith3Finger = 15,
        HoldWith4Finger = 16,
        PinchWith2Finger = 17,
        ZoomWith2Finger = 18,
        RotateWith2Finger = 19,
        SwipeRightWith1Finger = 20,
        SwipeLeftWith1Finger = 21,
        SwipeDownWith1Finger = 22,
        SwipeUpWith1Finger = 23,
        FlickRightWith1Finger = 24,
        FlickLeftWith1Finger = 25,
        FlickDownWith1Finger = 26,
        FlickUpWith1Finger = 27,
        SwipeRightWith2Finger = 28,
        SwipeLeftWith2Finger = 29,
        SwipeDownWith2Finger = 30,
        SwipeUpWith2Finger = 31,
        FlickRightWith2Finger = 32,
        FlickLeftWith2Finger = 33,
        FlickDownWith2Finger = 34,
        FlickUpWith2Finger = 35,
        SwipeRightWith3Finger = 36,
        SwipeLeftWith3Finger = 37,
        SwipeDownWith3Finger = 38,
        SwipeUpWith3Finger = 39,
        FlickRightWith3Finger = 40,
        FlickLeftWith3Finger = 41,
        FlickDownWith3Finger = 42,
        FlickUpWith3Finger = 43,
        SwipeRightWith4Finger = 44,
        SwipeLeftWith4Finger = 45,
        SwipeDownWith4Finger = 46,
        SwipeUpWith4Finger = 47,
        FlickLeftWith4Finger = 48,
        FlickRightWith4Finger = 49,
        FlickDownWith4Finger = 50,
        FlickUpWith4Finger = 51,
        Shake = 52,
        Navigation = 53
    }

    class ManifestController
    {
        private byte kDataPackageFileVersion = 1;
        private byte kSessionManifestFileVersion = 1;

        private MemoryStream mPackage;
        private MemoryStream mHead;
        private MemoryStream mContent;

        public ManifestController()
        {
            mHead       = new MemoryStream();
            mPackage    = new MemoryStream();
            mContent    = new MemoryStream();
        }

        // [0-1]=DataPackageFileSignature:word; //'H'+'A'
        // [2]=DataPackageFileVersion:byte;
        // [3-38]= SessionId:UUIDV4; //UUID to keep every session unique.
        private void writeArray(MemoryStream aMS, byte[] aBlock)
        {
            aMS.Write(aBlock, 0, aBlock.Length);
        }

        public void buildHeader()
        {
            mHead.WriteByte((byte)'H');
            mHead.WriteByte((byte)'A');
            mHead.WriteByte(kDataPackageFileVersion);

            mHead.Write(Detector.getSessionID(), 0, Detector.getSessionID().Length);
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
        public void buildDataPackage(GestureData aData)
        {
            mPackage.WriteByte((byte)'<');

            writeArray(mPackage, aData.ActionOrder);
            mPackage.WriteByte(aData.ActionID);
            writeArray(mPackage, aData.PosX);
            writeArray(mPackage, aData.PosY);
            writeArray(mPackage, aData.Param1);
            writeArray(mPackage, BitConverter.GetBytes(aData.ViewIDLenght));
            writeArray(mPackage, aData.ViewID);
            writeArray(mPackage, BitConverter.GetBytes(aData.ElementIDLenght));
            writeArray(mPackage, aData.ElementID);

            mPackage.WriteByte((byte)'>');
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
        public void buildSessionManifest()
        {
            mContent.WriteByte(kSessionManifestFileVersion);
            writeArray(mContent, Detector.getSessionID());
            writeArray(mContent, Detector.getSessionStartDate());
            writeArray(mContent, Detector.getSessionEndDate());
            writeArray(mContent, Detector.getUDID());
            writeArray(mContent, Detector.getResolutionX());
            writeArray(mContent, Detector.getResolutionY());
            mContent.WriteByte(Detector.ApiVersion);
            writeArray(mContent, Detector.ApiKey);
            writeArray(mContent, Detector.AppVersion);
            writeArray(mContent, Detector.OSVersion);
            writeArray(mContent, Detector.SystemLocale);

            mContent.WriteByte((byte)'>');
        }
    }
}
