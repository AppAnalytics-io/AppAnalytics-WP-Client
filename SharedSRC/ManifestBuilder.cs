using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppAnalytics
{
    using System.IO;
    using ManifestsDict = Dictionary<string, byte[]>;
    using SamplesDIct = SerializableDictionary<string, List<byte[]>>;

    static class ManifestBuilder
    {
        private static MemoryStream mPackage = new MemoryStream();
        private static MemoryStream mManifestStream = new MemoryStream();

        public const byte kDataPackageFileVersion = 1;
        public const byte kSessionManifestFileVersion = 1;

        static private void writeArray(MemoryStream aMS, byte[] aBlock)
        {
            aMS.Write(aBlock, 0, aBlock.Length);
        }

        static public void buildDataPackage(GestureData aData, SamplesDIct mSamples, object _lock)
        {
            mPackage.WriteByte((byte)'<');

            writeArray(mPackage, aData.ActionOrder);
            mPackage.WriteByte(aData.ActionID);
            writeArray(mPackage, aData.ActionTime);
            writeArray(mPackage, aData.PosX);
            writeArray(mPackage, aData.PosY);
            writeArray(mPackage, aData.Param1);

            writeArray(mPackage, BitConverter.GetBytes(aData.ViewIDLenght));
            writeArray(mPackage, aData.ViewID);
            writeArray(mPackage, BitConverter.GetBytes(aData.ElementIDLenght));
            writeArray(mPackage, aData.ElementID);

            mPackage.WriteByte((byte)'>');

            lock (_lock)
            {
                if (mSamples.ContainsKey(Detector.getSessionIDStringWithDashes()))
                {
                    mSamples[Detector.getSessionIDStringWithDashes()].Add(mPackage.ToArray());
                }
                else
                {
                    mSamples[Detector.getSessionIDStringWithDashes()] = new List<byte[]>();
                    mSamples[Detector.getSessionIDStringWithDashes()].Add(mPackage.ToArray());
                }
            } 
            mPackage.Dispose(); 

            mPackage = new MemoryStream();
        }

        static public void buildSessionManifest(ManifestsDict mManifests, object _lock)
        {
            mManifestStream.WriteByte((byte)'<');
            mManifestStream.WriteByte(kSessionManifestFileVersion);
            writeArray(mManifestStream, Detector.getSessionID());

            writeArray(mManifestStream, Detector.getSessionStartDate());

            writeArray(mManifestStream, Detector.getSessionEndDate());

            writeArray(mManifestStream, Detector.getUDID().Take(32).ToArray()); // 90 != 85 => cropping

            writeArray(mManifestStream, Detector.getResolutionX());
            writeArray(mManifestStream, Detector.getResolutionY());

            mManifestStream.WriteByte(Detector.ApiVersion);

            writeArray(mManifestStream, Detector.ApiKey);
            writeArray(mManifestStream, Detector.AppVersion);
            writeArray(mManifestStream, Detector.OSVersion);
            writeArray(mManifestStream, Detector.SystemLocale);

            mManifestStream.WriteByte((byte)'>');
            lock (_lock)
            {
                mManifests[Detector.getSessionIDString()] = mManifestStream.ToArray();
            }
            var t = mManifestStream.ToArray();
            mManifestStream.Dispose();
            mManifestStream = new MemoryStream();
        }
    }
}
