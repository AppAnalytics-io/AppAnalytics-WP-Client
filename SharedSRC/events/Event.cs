using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
 
namespace AppAnalytics
{
    [DataContract]
    //[XmlRoot("AAEvent")]
    internal class AAEvent : IEquatable<object>/*, IXmlSerializable*/
    {
        #region Members
        // private object _lockObj = new object();  

        [DataMember(Name = "indices", IsRequired = true)]
        private List<UInt32> mIndices = new List<UInt32>();

        [DataMember(Name = "time", IsRequired = true)]
        private List<Double> mTimeStamps = new List<Double>();

        [DataMember(Name = "description", IsRequired = true)]
        private String mDescription = "";

        [DataMember(Name = "params", IsRequired = false)]
        private Dictionary<string, string> mParameters = new Dictionary<string, string>();
        #endregion

        static internal AAEvent create(UInt32 aIndex, Double aTimeStamp, String aDescription, Dictionary<string, string> aParameters)
        {
            AAEvent newOne = new AAEvent();

            newOne.mIndices.Add(aIndex);
            newOne.mTimeStamps.Add(aTimeStamp);
            newOne.mDescription = aDescription;
            if (aParameters != null)
            {
                newOne.mParameters = aParameters.ToDictionary(entry => entry.Key,
                                                              entry => entry.Value);
            }

            return newOne;
        }

        static internal AAEvent create(List<UInt32> aIndeces, List<Double> aTimeStamps, String aDescription, Dictionary<string, string> aParameters)
        {
            AAEvent newOne = new AAEvent();

            Debug.Assert(aIndeces.Count != 0 && aTimeStamps.Count != 0);
           
            newOne.mIndices = aIndeces.ToList();
            newOne.mTimeStamps = aTimeStamps.ToList();
            newOne.mDescription = aDescription;
            if (aParameters != null)
            {
                newOne.mParameters = aParameters.ToDictionary(entry => entry.Key,
                                                              entry => entry.Value);
            }

            return newOne;
        }

        public void addIndex(UInt32 aIndex)
        {
//             lock (_lockObj)
//             {
            mIndices.Add(aIndex);
/*            }*/
        }

        public void addTimestamp(Double aTimestamp)
        {
//             lock(_lockObj)
//             {
            mTimeStamps.Add(aTimestamp);
/*            }*/
        }

        AAEvent() { }
        ~AAEvent() { }

        public string getJsonString()
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.Append('{');

            if (mIndices.Count > 0)
            {
                sb.Append(this.indicesToJSON(mIndices));
            }
            if (mParameters.Count > 0)
            {
                sb.Append(this.getJSONFromDict(mParameters));
            }
            if (mDescription.Length > 0)
            {
                sb.Append(string.Format("\"EventName\":\"{0}\",", mDescription));
            }
            if (mTimeStamps.Count > 0)
            {
                sb.Append(this.timestampsToJSON(mTimeStamps));
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string indicesToJSON(List<UInt32> aList)
        {
            //there may be performance issues
            StringBuilder dump = new StringBuilder();

            bool first = true;
            foreach (var it in aList)
            {
                if (first)
                    dump.Append(it.ToString());
                else
                    dump.Append("," + it.ToString());
            }
            return string.Format("\"ActionOrder\":[{0}],", dump.ToString());
        }

        private string timestampsToJSON(List<Double> aList)
        {
            //there may be performance issues
            StringBuilder dump = new StringBuilder();

            bool first = true;
            foreach (var it in aList)
            {
                if (first)
                    dump.Append(it.ToString());
                else
                    dump.Append("," + it.ToString(CultureInfo.InvariantCulture));
            }
            return string.Format("\"ActionTime\":[{0}]", dump.ToString());
        }
        private string getJSONFromDict(Dictionary<string,string> dict)
        {
            var entries = dict.Select(d =>
                string.Format("\"{0}\": \"{1}\"", d.Key, d.Value));

            return " \"EventParameters\":{" + string.Join(",", entries) + "},";
        }

        #region IEquatable Members
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as AAEvent);
        }

        public bool Equals(AAEvent obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            bool descriptionFlag = mDescription.Equals(obj.mDescription);
            bool paramsFlag = (mParameters.Count == obj.mParameters.Count
                                && !mParameters.Except(obj.mParameters).Any());

            return descriptionFlag && paramsFlag;
        }

        // tricky, but why not
        public override int GetHashCode()
        {
            return string.Format("{0}_{1}", mDescription.GetHashCode(), mParameters.GetHashCode()).GetHashCode();
        }
        #endregion

        public override string ToString()
        {
            if (mParameters.Count > 0)
            {
                string s = string.Join(";", mParameters.Select(x => x.Key + "=" + x.Value).ToArray());
                return String.Format("AA: Event [{0}] recorded;\n->Parameters: {1}",
                                    mDescription, s);
            }
            return String.Format("AA: Event [{0}] recorded.", mDescription);;
        }

//         #region IXmlSerializable Members
//         public System.Xml.Schema.XmlSchema GetSchema()
//         {
//             return null;
//         }
//    
//         public void ReadXml(System.Xml.XmlReader reader)
//         {
//             XmlSerializer indicesSerializer = new XmlSerializer(typeof(List<UInt32>));
//             XmlSerializer timeStampsSerializer = new XmlSerializer(typeof(List<Double>));
// 
//             XmlSerializer stringSerializer = new XmlSerializer(typeof(string));
// 
//             bool wasEmpty = reader.IsEmptyElement;
//             reader.Read();
// 
//             if (wasEmpty)
//                 return;
//             
//             reader.ReadStartElement("indices");
//             mIndices = (List<UInt32>)indicesSerializer.Deserialize(reader);
//             reader.ReadEndElement();
// 
//             reader.ReadStartElement("descr");
//             mDescription = (string)stringSerializer.Deserialize(reader);
//             reader.ReadEndElement();
// 
//             reader.ReadStartElement("timestamps");
//             mTimeStamps = (List<Double>)timeStampsSerializer.Deserialize(reader);
//             reader.ReadEndElement();
// 
//             reader.ReadStartElement("params");
//             while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
//             {
//                 reader.ReadStartElement("item");
// 
//                 reader.ReadStartElement("key");
//                 string key = (string)stringSerializer.Deserialize(reader);
//                 reader.ReadEndElement();
// 
//                 reader.ReadStartElement("value");
//                 string value = (string)stringSerializer.Deserialize(reader);
//                 reader.ReadEndElement();
// 
//                 mParameters.Add(key, value);
// 
//                 reader.ReadEndElement();
//                 reader.MoveToContent();
//             } 
//             reader.ReadEndElement();
//         }
// 
//         public void WriteXml(System.Xml.XmlWriter writer)
//         {
//             XmlSerializer indicesSerializer = new XmlSerializer(typeof(List<UInt32>));
//             XmlSerializer timeStampsSerializer = new XmlSerializer(typeof(List<Double>));
// 
//             XmlSerializer stringSerializer = new XmlSerializer(typeof(string));
// 
//             writer.WriteStartElement("indices");
//             indicesSerializer.Serialize(writer, mIndices);
//             writer.WriteEndElement();
// 
//             writer.WriteStartElement("descr");
//             stringSerializer.Serialize(writer, mDescription);
//             writer.WriteEndElement();
// 
//             writer.WriteStartElement("timestamps");
//             timeStampsSerializer.Serialize(writer, mTimeStamps);
//             writer.WriteEndElement();
// 
//             writer.WriteStartElement("params");
//             foreach (string key in mParameters.Keys)
//             {
//                 writer.WriteStartElement("item");
// 
//                 writer.WriteStartElement("key");
//                 stringSerializer.Serialize(writer, key);
//                 writer.WriteEndElement();
// 
//                 writer.WriteStartElement("value");
//                 string value = mParameters[key];
//                 stringSerializer.Serialize(writer, value);
//                 writer.WriteEndElement();
// 
//                 writer.WriteEndElement();
//             }
//             writer.WriteEndElement();
//         }
//         #endregion
    }
}
