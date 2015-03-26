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
    // TODO: remove sts hack.
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
            public byte[] Data = null;
            public AAFileType FileType = AAFileType.FTManifests;
            public Dictionary<string, List<object>> ListToDelete = null;
            public StateObject(byte[] pair, AAFileType aType, Dictionary<string, List<object>> aListToDelete)
            {
                Data = pair;
                FileType = aType;
                ListToDelete = aListToDelete;
            }
        }

        private static readonly Encoding encoding = Encoding.UTF8;

        public static bool MultipartFormDataPut(IPUTRequest aRequest, Dictionary<string, object> postParameters,
                    Dictionary<string, List<object>> aListToDelete, string boundary)
        {
            string formDataBoundary = boundary;
            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

            var aType = AAFileType.FTManifests;
            var fp = postParameters.ElementAt(0).Value as FileParameter;
            if (fp != null)
            { aType = fp.FileType; }

            return PutForm(aRequest, formData, aType, aListToDelete);
        }

        public static bool MultipartFormDataPut(IPUTRequest aRequest, FileParameter postParameters,
            Dictionary<string, List<object>> ListToDelete, string boundary)
        {
            var dict = new Dictionary<string, object>();
            dict.Add("-", postParameters);
            return MultipartFormDataPut(aRequest, dict, ListToDelete, boundary);
        } 
        //static List<Dictionary<string, List<object>>> sts = new List<Dictionary<string, List<object>>>();
       
        private static bool PutForm(IPUTRequest aRequest, byte[] formData, 
                    AAFileType aType, Dictionary<string, List<object>> ListToDelete)
        {
            var state = formData;
            var stateObj = new StateObject(state, aType, ListToDelete);
            //sts.Add(ListToDelete);

            aRequest.SendRequest(aRequest.GetRequestStreamCallback, stateObj, Sender.success);
//             HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;
// 
//             if (request == null)
//             {
//                 throw new NullReferenceException("not a http request");
//             }
// 
//             request.Method = "PUT";
//             request.ContentType = contentType;
// #if SILVERLIGHT
//             request.UserAgent = userAgent;
//             request.ContentLength = formData.Length;
// #else
//             SetHeader(request, "UserAgent", userAgent);
// //             SetHeader(request, "ContentLength", formData.Length.ToString());
// #endif
// 
//             var st = request.ToString();
// 
//             var state = new KeyValuePair<HttpWebRequest, byte[]>(request, formData);
//             var stateObj = new StateObject(state, aType, ListToDelete);
//             sts.Add(ListToDelete);
// 
//             var result = request.BeginGetRequestStream(GetRequestStreamCallback, stateObj);

            return true;
        }

        public static string createHeader(string boundary, FileParameter fileToUpload)
        {
            return string.Format("--{0}\r\nContent-Disposition: form-data; name={1}\r\nContent-Type: {2}\r\n\r\n",// \r\n",
                                boundary,
                                fileToUpload.typeToString(),
                                fileToUpload.ContentType);
        }

        public static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));
                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    string header = createHeader(boundary, fileToUpload);

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
    }
}
