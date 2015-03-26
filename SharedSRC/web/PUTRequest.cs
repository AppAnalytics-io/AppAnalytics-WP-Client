using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AppAnalytics
{
    internal interface IPUTRequest
    { 
        void    GetRequestStreamCallback(IAsyncResult asynchronousResult);

        void    SetHeader(string aName, string aValue);
        IAsyncResult SendRequest(AsyncCallback aCallback, object aState,
                                 Defaults.ResultCallback aResultCallback);
    }
     
    internal class PUTRequest : IPUTRequest
    { 
        HttpWebRequest mWebRequest = null;

        private Defaults.ResultCallback fResultCallback = null;

        public HttpWebRequest GetWebRequest() { return mWebRequest; }

        public PUTRequest(string aURL, string aContentType)
        {
            mWebRequest = WebRequest.Create(aURL) as HttpWebRequest;

            if (mWebRequest == null)
            {
                throw new NullReferenceException("not a http request");
            }
            mWebRequest.CookieContainer = new CookieContainer(); 
            mWebRequest.Method = "PUT";
            mWebRequest.ContentType = aContentType;
        }

        public IAsyncResult SendRequest(AsyncCallback aCallback, object aState,
                                 Defaults.ResultCallback aResultCallback)
        {
            fResultCallback = aResultCallback;
            return mWebRequest.BeginGetRequestStream(aCallback, aState);
        } 

        public void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            var stateObj = asynchronousResult.AsyncState as MultipartUploader.StateObject;
            Debug.Assert(stateObj != null);

            byte[] dataToSend = stateObj.Data;
            var request = mWebRequest;

            Debug.Assert(request != null); 
            // End the operation -> exception
            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            // Write to the request stream.
            postStream.Write(dataToSend, 0, dataToSend.Length);
            postStream.Dispose();
            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), stateObj);
        }

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            var stateObj = asynchronousResult.AsyncState as MultipartUploader.StateObject;
            Debug.Assert(stateObj != null);

            HttpWebRequest request = mWebRequest;//stateObj.RequestDataPair.Key as HttpWebRequest;
            HttpWebResponse response = null;
            Stream streamResponse = null;
            StreamReader streamRead = null;

            try
            {
                response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                streamResponse = response.GetResponseStream();
                streamRead = new StreamReader(streamResponse);

                string responseString = streamRead.ReadToEnd();
                Debug.WriteLine("[sending.. response:]" + responseString);

                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    fResultCallback(true, stateObj.FileType, stateObj.ListToDelete); 
                }
                else
                {
                    fResultCallback(false, stateObj.FileType, stateObj.ListToDelete);
                }
            }
            catch (Exception)
            {
                fResultCallback(false, stateObj.FileType, stateObj.ListToDelete);
            }
            finally
            {
                if (null != streamResponse) streamResponse.Dispose();
                if (null != streamRead) streamRead.Dispose();
                if (null != response) response.Dispose();
            }
        }

        public void SetHeader(string aName, string aValue)
        {
#if SILVERLIGHT
            if (aName == "User-Agent")
                mWebRequest.UserAgent = aValue;
            else if (aName == "Content-Length")
                mWebRequest.ContentLength = long.Parse(aValue);
#else
            // Retrieve the property through reflection.
            // Check if the property is available.
            try
            {
                PropertyInfo PropertyInfo = mWebRequest.GetType().GetRuntimeProperty(aName.Replace("-", string.Empty));
                {
                    mWebRequest.Headers[aName] = aValue;
                }
            }
            catch (Exception) { }
#endif
        }
    }
}
