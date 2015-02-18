using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TouchLib
{
    internal static class MultipartUploader
    {
       // private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static readonly Encoding encoding = Encoding.UTF8;
        private static bool mFlag = false;

        public static bool MultipartFormDataPost(string postUrl, string userAgent, Dictionary<string, object> postParameters, bool isManifest)
        {
            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary, isManifest);
 
            return PostForm(postUrl, userAgent, contentType, formData);
        }

        private static bool PostForm(string postUrl, string userAgent, string contentType, byte[] formData)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;
 
            if (request == null)
            {
                throw new NullReferenceException("not a http request");
            }

            request.Method = "PUT";
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.CookieContainer = new CookieContainer();
            request.ContentLength = formData.Length;

            var state = new KeyValuePair<HttpWebRequest, byte[]>(request, formData);
            var result = request.BeginGetRequestStream(GetRequestStreamCallback, state);

            var tmp = mFlag;
            mFlag = false;
            return tmp;
        }

        static int _packageIndex = 0;
        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary, bool isManifest)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = true;
 
            foreach (var param in postParameters)
            {
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));
 
                needsCLRF = true;
                string fname = "";
                if (isManifest)
                {
                    fname = Detector.getSessionID() + ".manifest";
                }
                else
                {
                    fname = string.Format("{0}_{1}.datapackage", Detector.getSessionIDString(), _packageIndex.ToString());
                    _packageIndex++;
                }
 
                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;
  
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        isManifest ? "Manifest" : "Sample",
                        fname,
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
            formDataStream.Close();
 
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

            Console.WriteLine("Please enter the input data to be posted:");

            // Write to the request stream.
            postStream.Write(dataToSend, 0, dataToSend.Length);
            postStream.Close();

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
                if (null != streamResponse) streamResponse.Close();
                if (null != streamRead) streamRead.Close();
                if (null != response) response.Close();
            }

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                Sender.success();
                Debug.WriteLine("request succeed");
            }
            else Sender.fail();
            //allDone.Set();
        }
    }
}
