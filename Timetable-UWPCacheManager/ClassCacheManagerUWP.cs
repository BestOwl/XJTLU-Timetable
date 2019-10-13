using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timetable_Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Timetable_UWPCacheManager
{
    public class ClassCacheManagerUWP : ClassCacheManager
    {
        public static ClassCacheManager Instance = new ClassCacheManagerUWP();

        public override async Task<bool> DeleteCacheFileAsync(string accountId, DateTime startOfWeek)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(GetCacheFileName(accountId, startOfWeek));
            if (file != null)
            {
                await file.DeleteAsync();
                return true;
            }

            return false;
        }

        public override async Task<Stream> GetCacheFileAsync(string accountId, DateTime startOfWeek)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                GetCacheFileName(accountId, startOfWeek), CreationCollisionOption.OpenIfExists);
            return (await file.OpenAsync(FileAccessMode.ReadWrite)).AsStream();
        }
    }
}
