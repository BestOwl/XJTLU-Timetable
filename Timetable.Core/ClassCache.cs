using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timetable.Core
{
    public class ClassCache
    {
        public DateTime StartOfWeek { get; set; }
        public List<Class> ClassList { get; set; } = new List<Class>();
        public int ReminderSelectionIndex { get; set; }

        [JsonIgnore]
        public FileInfo Path { get; set; }

        public ClassCache(DateTime startOfWeek, List<Class> classList, FileInfo path)
        {
            StartOfWeek = startOfWeek;
            ClassList = classList;
            Path = path;
        }

        public async Task SaveCache()
        {
            await Task.Run(() =>
            {
                if (!Path.Directory.Exists)
                {
                    Path.Directory.Create();
                }

                File.WriteAllText(Path.FullName, JsonConvert.SerializeObject(this, Formatting.Indented));
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
