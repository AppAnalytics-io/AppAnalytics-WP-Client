using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
#if SILVERLIGHT
using System.IO.IsolatedStorage;
#else
using Windows.Storage;
#endif

namespace AppAnalytics
{
    internal interface IFileSystemHelper
    {
        bool doesFileExist(string aFName);

        Stream getFileStream(string aFName, bool aRead);

        void deleteFile(string aFname);
    }

#if SILVERLIGHT
    internal class FileSystemHelper : IFileSystemHelper
    {
        public bool doesFileExist(string aFName)
        {
            IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication();
            return iStorage.FileExists(aFName);
        }

        public void deleteFile(string aFname)
        {
            try
            {
                IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication(); 
                iStorage.DeleteFile(aFname);
            }
            catch (Exception) {}
         }

        public Stream getFileStream(string aFName, bool aRead)
        {
            IsolatedStorageFile iStorage = IsolatedStorageFile.GetUserStoreForApplication(); 
            Stream fstream = null;

            try
            {
                if (aRead && doesFileExist(aFName))
                { 
                    fstream = iStorage.OpenFile(aFName, FileMode.Open);
                }
                else
                {
                    fstream = iStorage.OpenFile(aFName, FileMode.Create);
                }
            }
            catch (Exception) { }

            return fstream;
        } 
    }

#else
    internal class FileSystemHelper : IFileSystemHelper
    {
        public Stream getFileStream(string aFName, bool aRead)
        {
            var str = getFileStreamAsync(aFName, aRead).Result;
            return str;
        }

        public async Task deleteFileAsync(string aFname)
        {
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var file = await folder.GetFileAsync(aFname);
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception)
            {
                // file does not exist
            }
        }

        public void deleteFile(string aFname)
        {
            deleteFileAsync(aFname).Wait();
        }
         
        public bool doesFileExist(string aFName)
        {
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            return doesFileExistAsync(aFName, folder).Result;
        }

        public async Task<Stream> getFileStreamAsync(string aFName, bool aRead)
        {
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Stream fstream = null;

            try
            {
                if (aRead && doesFileExist(aFName))
                {
                    var file = await folder.GetFileAsync(aFName);
                    fstream = await file.OpenStreamForReadAsync();
                }
                else
                {
                    var file = await folder.CreateFileAsync(aFName,
                                                            CreationCollisionOption.ReplaceExisting);
                    fstream = await file.OpenStreamForWriteAsync();
                }
            }
            catch (Exception) { }

            return fstream;
        }

        async Task<bool> doesFileExistAsync(string fileName, StorageFolder folder)
        {
            try
            {
                await folder.GetFileAsync(fileName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
#endif
}
 