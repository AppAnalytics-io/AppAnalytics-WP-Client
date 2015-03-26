using System;
using System.Windows;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace UTAppAnalytics
{
    [TestClass]
    public class UDIDGenTester
    {
        internal AppAnalytics.FileSystemHelper mFSH = new AppAnalytics.FileSystemHelper();

        [TestInitialize()]
        public void initTest()
        {
            mFSH.deleteFile(AppAnalytics.UUID.UDIDGen.kUDIDFName);
        }

        [TestMethod]
        public void UDIDChecker()
        {
            var inst = AppAnalytics.UUID.UDIDGen.Instance;

            inst.init();
            Assert.IsTrue(mFSH.doesFileExist(AppAnalytics.UUID.UDIDGen.kUDIDFName), "file should be created");

            var copyUDID = new Guid(inst.UDIDRaw.ToByteArray()); 
            var copySESSION = new Guid(inst.SessionIDRaw.ToByteArray());

            inst.init();

            Assert.AreNotEqual<Guid>(copySESSION, inst.SessionIDRaw);
            Assert.AreEqual<Guid>(copyUDID, inst.UDIDRaw);

            mFSH.deleteFile(AppAnalytics.UUID.UDIDGen.kUDIDFName);
            inst.init();
            Assert.AreNotEqual<Guid>(copySESSION, inst.SessionIDRaw);
            Assert.AreNotEqual<Guid>(copyUDID, inst.UDIDRaw, "should be different after re-installation");
        }  
    }
}
