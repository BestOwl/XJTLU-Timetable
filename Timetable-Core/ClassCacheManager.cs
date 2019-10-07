using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Timetable_Core
{
    public abstract class ClassCacheManager
    {
        public string GetCacheFileName(string studentId, DateTime startOfWeek)
        {
            return string.Format(@"cache\{0}\timetable-{1}.json", studentId, WeekHelper.GetDateStr(startOfWeek));
        }

        public abstract string GetCacheFilePath(string studentId, DateTime startOfWeek);

        public async Task<ClassCache> LoadCache(DateTime startOfWeek, string studentId)
        {
            if (string.Equals("0000-0000-0000-0000", studentId))
            {
                return LoadTestCache();
            }

            await Task.Run(() =>
            {
                string path = GetCacheFilePath(studentId, startOfWeek);
                if (!File.Exists(path))
                {
                    return null;
                }

                try
                {
                    return JsonConvert.DeserializeObject<ClassCache>(File.ReadAllText(path));
                }
                catch
                {
                    File.Delete(path);
                    return null;
                }
            });

            return null;
        }

        // To meet the Microsoft Store submission requirement.
        public static ClassCache LoadTestCache()
        {
            ClassCache ret = new ClassCache(WeekHelper.GetStartDayOfWeek(), new List<Class>(), "0000-0000-0000-0000");
            ret.ClassList.Add(new Class
            {
                ModuleCode = "EAP021",
                Location = "Foundation Building-FB321",
                StartTime = new DateTime(2019, 09, 02, 09, 00, 00),
                EndTime = new DateTime(2019, 09, 09, 11, 00, 00)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "CCT007",
                Location = "Science Building Block-SB101",
                StartTime = new DateTime(2019, 09, 02, 15, 00, 00),
                EndTime = new DateTime(2019, 09, 02, 17, 00, 00)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "MTH013",
                Location = "Science Building Block B-SB222",
                StartTime = new DateTime(2019, 09, 03, 15, 00, 00),
                EndTime = new DateTime(2019, 09, 03, 17, 00, 00)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "CSE001",
                Location = "Science Building Block A-SA101",
                StartTime = new DateTime(2019, 09, 04, 11, 00, 00),
                EndTime = new DateTime(2019, 09, 04, 13, 00, 00)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "EAP021",
                Location = "Foundation Building-FB321",
                StartTime = new DateTime(2019, 09, 05, 11, 00, 00),
                EndTime = new DateTime(2019, 09, 05, 13, 00, 00)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "MTH013",
                Location = "Science Building Block B-SB222",
                StartTime = new DateTime(2019, 09, 05, 15, 00, 00),
                EndTime = new DateTime(2019, 09, 05, 17, 00, 00)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "MTH007",
                Location = "Science Building Block D-SD325",
                StartTime = new DateTime(2019, 09, 06, 11, 00, 00),
                EndTime = new DateTime(2019, 09, 06, 13, 00, 00)
            });
            return ret;
        }
    }
}
