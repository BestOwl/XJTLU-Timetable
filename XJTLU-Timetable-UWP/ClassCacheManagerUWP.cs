using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timetable_Core;
using Windows.Storage;

namespace XJTLU_Timetable_UWP
{
    public class ClassCacheManagerUWP : ClassCacheManager
    {
        public static ClassCacheManager Instance = new ClassCacheManagerUWP();

        public override string GetCacheFilePath(string studentId, DateTime startOfWeek)
        {
            return ApplicationData.Current.LocalFolder.Path + @"\" + GetCacheFileName(studentId, startOfWeek);
        }
    }
}
