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
        public class FileParameter
        {
            public static  string typeToString(AAFileType aFT)
            {
                switch (aFT)
                {
                    case AAFileType.FTSamples:
                        return "Sample";
                    case AAFileType.FTManifests:
                        return "Manifest";
                    case AAFileType.FTEvents:
                        return "Events";
                    default:
                        return "File";
                }
            }
            public string typeToString()
            {
                return typeToString(FileType);
            }

            public AAFileType FileType { get; set; }
            public UInt32 Count { get; set; }
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }

            public FileParameter(byte[] file, string filename, AAFileType aType) : this(file, filename, 1, aType) { }
            public FileParameter(byte[] file, string filename, UInt32 count, AAFileType aType) : this(file, filename, "application/octet-stream", count, aType) { }
            public FileParameter(byte[] file, string filename, string contenttype, AAFileType aType) : this(file, filename, contenttype, 1, aType) { }

            public FileParameter(byte[] file, string filename, string contenttype, UInt32 count, AAFileType aType)
            {
                File        = file;
                FileName    = filename;
                ContentType = contenttype;
                Count       = count;
                FileType    = aType;
            }
        }

        public class StateObject
        {
            public KeyValuePair<HttpWebRequest, byte[]> RequestDataPair = new KeyValuePair<HttpWebRequest,byte[]>(null, null);
            public AAFileType FileType = AAFileType.FTManifests;
            public Dictionary<string, List<object>> ListToDelete = null;
            public StateObject(KeyValuePair<HttpWebRequest, byte[]> pair, AAFileType aType, Dictionary<string, List<object>> aListToDelete)
            {
                RequestDataPair = pair;
                FileType = aType;
                ListToDelete = aListToDelete;
            }
        }

        private static readonly Encoding encoding = Encoding.UTF8;

        public static bool MultipartFormDataPut(string postUrl, string userAgent, Dictionary<string, object> postParameters, Dictionary<string, List<object>> aListToDelete)
        {
            var d = Guid.NewGuid();

            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

            var aType = AAFileType.FTManifests;
            var fp = postParameters.ElementAt(0).Value as FileParameter;
            if (fp != null)
            { aType = fp.FileType; }

            return PutForm(postUrl, userAgent, contentType, formData, aType, aListToDelete);
        }

        public static bool MultipartFormDataPut(string postUrl, string userAgent, FileParameter postParameters, Dictionary<string, List<object>> ListToDelete)
        {
            var dict = new Dictionary<string, object>();
            dict.Add("-", postParameters);
            return MultipartFormDataPut(postUrl, userAgent, dict, ListToDelete);
        }
#if UNIVERSAL
        // winrt version of HttpWebRequest doesn't have UserAgent as header by default.
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
        static List<Dictionary<string, List<object>>> sts = new List<Dictionary<string, List<object>>>();
        private static bool PutForm(string postUrl, string userAgent, string contentType,
                                    byte[] formData, AAFileType aType, Dictionary<string, List<object>> ListToDelete)
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
//             SetHeader(request, "UserAgent", userAgent);
//             SetHeader(request, "ContentLength", formData.Length.ToString());
#endif
            request.CookieContainer = new CookieContainer();

            var st = request.ToString();

            var state = new KeyValuePair<HttpWebRequest, byte[]>(request, formData);
            var stateObj = new StateObject(state, aType, ListToDelete);
            sts.Add(ListToDelete);
            //
            Sender.success(aType, ListToDelete);
            return true;
            //
            var result = request.BeginGetRequestStream(GetRequestStreamCallback, stateObj);

            return true;
        }


        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = true;

            foreach (var param in postParameters)
            {
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    string header =
                          string.Format("--{0}\r\nContent-Disposition: form-data; name={1}\r\nContent-Type: {2}\r\n\r\n",// \r\n",
                          boundary,
                          fileToUpload.typeToString(),
                          fileToUpload.ContentType);

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

        private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            var stateObj = asynchronousResult.AsyncState as StateObject;
            Debug.Assert(stateObj != null);

            KeyValuePair<HttpWebRequest, byte[]> state = stateObj.RequestDataPair;
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
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), stateObj);
        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            var stateObj = asynchronousResult.AsyncState as StateObject;
            Debug.Assert(stateObj != null);

            HttpWebRequest request = stateObj.RequestDataPair.Key;
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

                    string responseString = streamRead.ReadToEnd() + e.ToString();
                    //Debug.WriteLine("[response:]" + responseString);
                }
                //Debug.WriteLine("exception in response callback :" + e.ToString());
            }
            finally
            {
                if (null != streamResponse) streamResponse.Dispose();
                if (null != streamRead) streamRead.Dispose();
                if (null != response) response.Dispose();
            }

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                Sender.success(stateObj.FileType, stateObj.ListToDelete);
                //Debug.WriteLine("request succeed => " + FileParameter.typeToString(stateObj.FileType));
            }
            else
            {
                Sender.fail();
                //Debug.WriteLine("request failed. retry");
            }
        }
    }
}
