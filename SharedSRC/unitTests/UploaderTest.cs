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
    internal class TstPUTRequest : AppAnalytics.IPUTRequest
    {
        public bool CallbackWasTaken = false;
        public bool MethodSendWasCalled = false;
        public void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            CallbackWasTaken = true;
        }

        public void SetHeader(string aName, string aValue) { }
        public IAsyncResult SendRequest(AsyncCallback aCallback, object aState,
                                 AppAnalytics.Defaults.ResultCallback aResultCallback)
        {
            aCallback(null);
            MethodSendWasCalled = true;
            return null;
        }
    }

    [TestClass]
    public class UploaderTesterTester
    {
        [TestMethod] 
        public void UploaderShouldUseCustomPUTRequestClass()
        {
            TstPUTRequest t = new TstPUTRequest();
            
            var fp = new AppAnalytics.MultipartUploader.FileParameter(new byte[]{1,1,1},"",
                                        AppAnalytics.AAFileType.FTManifests);

            AppAnalytics.MultipartUploader.MultipartFormDataPut(t, fp, new Dictionary<string, List<object>>(), "---");

            Assert.IsTrue(t.CallbackWasTaken, "Uploader should use PURequest.GetRequestStreamCallback "
                                    + "as parameter to method SendRequest");

            Assert.IsTrue(t.MethodSendWasCalled, "SendRequest was not called.");
        } 
    }
}
