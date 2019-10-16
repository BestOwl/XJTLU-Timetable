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

        public ClassCache(DateTime startOfWeek, List<Class> classList)
        {
            StartOfWeek = startOfWeek;
            ClassList = classList;
        }

        public async Task SaveCache(Stream fileStream)
        {
            if (fileStream == null)
            {
                return;
            }

            using (fileStream)
            {
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    await writer.WriteAsync(JsonConvert.SerializeObject(this, Formatting.Indented));
                }
            }
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
