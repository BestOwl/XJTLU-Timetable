using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timetable_Core
{
    public class ClassCache
    {
        public DateTime StartOfWeek { get; set; }
        public List<Class> ClassList { get; set; } = new List<Class>();
        public int ReminderSelectionIndex { get; set; }

        [JsonIgnore]
        public string CacheFilePath;

        public ClassCache(DateTime startOfWeek, List<Class> classList, string cacheFilePath)
        {
            StartOfWeek = startOfWeek;
            ClassList = classList;
            CacheFilePath = cacheFilePath;
        }

        public async Task SaveCache()
        {
            await Task.Run(() =>
            {
                if (!File.Exists(CacheFilePath))
                {
                    File.Create(CacheFilePath);
                }
                File.WriteAllText(CacheFilePath, JsonConvert.SerializeObject(this));
            });
        }

        public bool IsLatestCache(List<Class> latestClassList, int reminderIndex)
        {
            if (ClassList.Count != latestClassList.Count)
            {
                return false;
            }
            if (ReminderSelectionIndex != reminderIndex)
            {
                return false;
            }

            for (int i = 0; i < ClassList.Count; i++)
            {
                if (!ClassList[i].IsSameClass(latestClassList[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
