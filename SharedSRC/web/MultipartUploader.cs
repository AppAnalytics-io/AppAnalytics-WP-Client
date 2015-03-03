using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppAnalytics
{
    internal static class MultipartUploader
    {
       // private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static readonly Encoding encoding = Encoding.UTF8;
        //private static bool mFlag = false;

        public static bool MultipartFormDataPost(string postUrl, string userAgent, Dictionary<string, object> postParameters, bool isManifest)
        {
            var d = Guid.NewGuid();

            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary, isManifest);
 
            return PostForm(postUrl, userAgent, contentType, formData);
        }
#if UNIVERSAL
        static private void SetHeader(HttpWebRequest Request, string Header, string Value)
        {
            // Retrieve the property through reflection.
            PropertyInfo PropertyInfo = Request.GetType().GetRuntimeProperty(Header.Replace("-", string.Empty));
            // Check if the property is available.
            try
            {
//                 if (PropertyInfo != null)
//                 {
//                     // Set the value of the header.
//                     PropertyInfo.SetValue(Request, Value, null); 
//                 }
//                 else
                {
                    // Set the value of the header.
                    Request.Headers[Header] = Value; 
                }
            }
            catch { }
        }
#endif

        private static bool PostForm(string postUrl, string userAgent, string contentType, byte[] formData)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;
 
            if (request == null)
            {
                throw new NullReferenceException("not a http request");
            }

            request.Method = "PUT";
            request.ContentType = contentType;
#if SILVERLIGHT
            request.UserAgent = userAgent;
            request.ContentLength = formData.Length;
#else
            SetHeader(request, "UserAgent", userAgent);
            SetHeader(request, "ContentLength", formData.Length.ToString());
#endif
            request.CookieContainer = new CookieContainer();

            var st = request.ToString();

            var state = new KeyValuePair<HttpWebRequest, byte[]>(request, formData);
            var result = request.BeginGetRequestStream(GetRequestStreamCallback, state);

            return true;
        }

        //static int _packageIndex = 0;
        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary, bool isManifest)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = true;
 
            foreach (var param in postParameters)
            {
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));
  
//                 string fname = "";
//                 if (isManifest)
//                 {
//                     fname = Detector.getSessionIDString() + ".manifest";
//                 }
//                 else
//                 {
//                     fname = string.Format("{0}_{1}.datapackage", Detector.getSessionIDString(), _packageIndex.ToString());
//                     _packageIndex++;
//                 }
 
                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

//                     string header =
//                       string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n",// \r\n",
//                       boundary,
//                       isManifest ? "Manifest" : "Sample",
//                       fname,
//                       "application/octet-stream");

                    string header =
                      string.Format("--{0}\r\nContent-Disposition: form-data; name={1}\r\nContent-Type: {2}\r\n\r\n",// \r\n",
                      boundary,
                      isManifest ? "Manifest" : "Sample",
                      "application/octet-stream");
 
                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));
 
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                } 
            }
 
            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));
 
            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Dispose();
 
            return formData;
        }
 
        public class FileParameter
        {
            public byte[] File { get ; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }

        private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            KeyValuePair<HttpWebRequest, byte[]> state = (KeyValuePair<HttpWebRequest, byte[]>)asynchronousResult.AsyncState;
            var request = state.Key;
            var dataToSend = state.Value;
            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            // Write to the request stream.
            postStream.Write(dataToSend, 0, dataToSend.Length);
#if SILVERLIGHT
            postStream.Close();
#else
            postStream.Dispose();
#endif

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
            HttpWebResponse response = null;
            Stream streamResponse = null;
            StreamReader streamRead = null;

            try
            {
                response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                streamResponse = response.GetResponseStream();
                streamRead = new StreamReader(streamResponse);

                string responseString = streamRead.ReadToEnd();
                Debug.WriteLine("[response:]" + responseString);
            }
            catch (Exception e)
            {
                if (response != null)
                {
                    streamResponse = response.GetResponseStream();
                    streamRead = new StreamReader(streamResponse);

                    string responseString = streamRead.ReadToEnd();
                    Debug.WriteLine("[response:]" + responseString);
                }
                Debug.WriteLine("exception in response callback :" + e.ToString());
            }
            finally
            {
                if (null != streamResponse) streamResponse.Dispose();
                if (null != streamRead) streamRead.Dispose();
                if (null != response) response.Dispose();
            }

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                Sender.success();
                Debug.WriteLine("request succeed");
            }
            else
            {
                Sender.fail();
                Debug.WriteLine("request failed. retry");
            }
            //allDone.Set();
        }
    }
}
