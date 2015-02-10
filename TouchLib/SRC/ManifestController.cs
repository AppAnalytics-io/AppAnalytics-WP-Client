using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Buffer;
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

            byteStr.Append( Detector.)

            return byteStr.ToString();
        }

        public String buildDataPackage(GestureData data)
        {
            StringBuilder byteStr = new StringBuilder("<");

            // - package -

            return byteStr.Append(">").ToString();
        }

        public String buildSessionManifest()
        {
            StringBuilder byteStr = new StringBuilder("<");

            // - session -

            return byteStr.Append(">").ToString();
        }
    }
}
