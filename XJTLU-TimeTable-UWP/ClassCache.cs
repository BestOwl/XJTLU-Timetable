using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace XJTLU_TimeTable_UWP
{
    public class ClassCache
    {
        public DateTime StartOfWeek { get; set; }
        public List<Class> ClassList { get; set; } = new List<Class>();
        public int ReminderSelectionIndex { get; set; }
        public string UserName { get; set; }

        [JsonIgnore]
        public string CacheFileName;

        public ClassCache(DateTime startOfWeek, List<Class> classList, string studenId)
        {
            StartOfWeek = startOfWeek;
            ClassList = classList;
            CacheFileName = GetCacheFileName(studenId, startOfWeek);
        }

        public static string GetCacheFileName(string studentId, DateTime startOfWeek)
        {
            return string.Format(@"cache\{0}\timetable-{1}.json", studentId, WeekHelper.GetDateStr(startOfWeek));
        }

        public async Task SaveCache()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(CacheFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(this));
        }

        public static async Task<ClassCache> LoadCache(DateTime startOfWeek, string studentId)
        {
            if (string.Equals("0000-0000-0000-0000", studentId))
            {
                return LoadTestCache();
            }

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(GetCacheFileName(studentId, startOfWeek), CreationCollisionOption.OpenIfExists);
            if (file == null)
            {
                return null;
            }
            try
            {
                return JsonConvert.DeserializeObject<ClassCache>(await FileIO.ReadTextAsync(file));
            }
            catch
            {
                await file.DeleteAsync();
                return null;
            }
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
