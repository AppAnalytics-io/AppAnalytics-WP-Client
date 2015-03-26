using System;
using System.Windows;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.IO;
using System.Xml.Serialization;

namespace UTAppAnalytics
{
    [TestClass]
    public class SerializableDictTest
    {
        [TestMethod]
        public static void StoreRestoreDictionaryCheck()
        {
            SerializableDictionary<string, List<byte[]>> original = new SerializableDictionary<string, List<byte[]>>();
            original.Add("tst", new List<byte[]>{BitConverter.GetBytes(42)});

            XmlSerializer xmlSerial = new XmlSerializer(typeof(SerializableDictionary<string, List<byte[]>>));

            var stream = new MemoryStream();
            xmlSerial.Serialize(stream, original);

            stream.Seek(0, SeekOrigin.Begin);
            object t = xmlSerial.Deserialize(stream);

            var copy = t as SerializableDictionary<string, List<byte[]>>;

            bool flag = true;

            if (original.Count != copy.Count)
            {
                flag = false;
            }
            else if (original["tst"].FirstOrDefault() != copy["tst"].FirstOrDefault())
            {
                flag = false;
            }

            Assert.IsTrue(flag, "Dictionary should be the same after serialization and deserialization");
            stream.Dispose();
        }
    }
}
