using Microsoft.Exchange.WebServices.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Timetable.Core
{
    public class ClassCacheManager
    {
        public ExchangeService Service;
        public bool IsTestAccount = false;

        private const int _DefaultWeekCount = 3;

        public static ClassCacheManager Instance = new ClassCacheManager();

        public ClassCacheManager()
        {
            Service = new ExchangeService();
            Service.Url = new Uri("https://mail.xjtlu.edu.cn/EWS/Exchange.asmx");
        }

        public void SetupExchangeAccount(XJTLUAccount account)
        {
            Service.Credentials = new WebCredentials(account.Username, account.Password, "xjtlu.edu.cn");
        }

        public static string GetCacheFileName(string accountId, DateTime startOfWeek)
        {
            return string.Format(@"cache\{0}\timetable-{1}.json", accountId, WeekHelper.GetDateStr(startOfWeek));
        }

        public static string GetCacheFilePath(string accountId, DateTime startOfWeek)
        {
            return FileSystem.CacheDirectory + "/" + GetCacheFileName(accountId, startOfWeek);
        }

        public async Task<ClassCache> LoadCache(DateTime startOfWeek, string accountId)
        {
            if (IsTestAccount)
            {
                return LoadTestCache();
            }

            string path = GetCacheFilePath(accountId, startOfWeek);
            return await System.Threading.Tasks.Task.Run(() =>
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                try
                {
                    ClassCache ret = JsonConvert.DeserializeObject<ClassCache>(File.ReadAllText(path));
                    ret.Path = new FileInfo(path);
                    return ret;
                }
                catch (JsonException)
                {
                    File.Delete(path);
                    return null;
                }
            });
        }

        public async Task<List<Class>> LoadClassListFromCache(DateTime startOfWeek, string accountId, int weekCount = _DefaultWeekCount)
        {
            List<Class> ret = new List<Class>();
            DateTime start = startOfWeek;
            for (int i = 0; i < weekCount; i++)
            {
                ClassCache cache = await LoadCache(start, accountId);
                if (cache != null)
                {
                    ret.AddRange(cache.ClassList);
                }
            }
            return ret;
        }

        public async Task<(bool TokenExpired, List<ClassCache> classCaches)> UpdateTimetable(string accountId, string token, int reminderIndex, int weekCount = _DefaultWeekCount)
        {
            DateTime startOfWeek = WeekHelper.GetStartDayOfWeek();
            DateTime endOfWeek = startOfWeek.AddDays(WeekHelper.Interval_EndOfWeek);
            List<ClassCache> caches = new List<ClassCache>();
            for (int i = 0; i < weekCount; i++)
            {
                var result = await GetTimetable(startOfWeek, endOfWeek, accountId, token);
                if (result.TokenExpired)
                {
                    return (true, null);
                }

                ClassCache cache = await LoadCache(startOfWeek, accountId);
                if (result.Classes != null)
                {
                    cache = await UpdateTimetableInternal(result.Classes, cache, startOfWeek, accountId, reminderIndex);
                }
                caches.Add(cache);

                startOfWeek = startOfWeek.AddDays(WeekHelper.Interval_StartOfNextWeek);
                endOfWeek = endOfWeek.AddDays(WeekHelper.Interval_StartOfNextWeek);
            }

            return (false, caches);
        }

        /// <summary>
        /// Compare provided class list and local cache to determin which one has the latest class list, then update to calendar.
        /// </summary>
        /// <param name="table">Provided class list</param>
        /// <param name="cache">Local cache</param>
        /// <param name="startOfWeek">Start date of the week</param>
        /// <param name="accountId">XJTLU Account Portal Internal ID</param>
        /// <param name="reminderIndex">Seleted reminder index</param>
        /// <returns>The updated cache with latest class list</returns>
        private async Task<ClassCache> UpdateTimetableInternal(List<Class> table, ClassCache cache, DateTime startOfWeek, string accountId, int reminderIndex)
        {
            ClassCache ret = cache;
            bool flag = false; // need to update

            if (ret != null)
            {
                if (!ret.IsLatestCache(table, reminderIndex))
                {
                    flag = true;

                    ret.ClassList = table;
                    ret.ReminderSelectionIndex = reminderIndex;
                }
            }
            else
            {
                flag = true;
                ret = new ClassCache(startOfWeek, table,  new FileInfo(GetCacheFilePath(accountId, startOfWeek)));
                ret.ReminderSelectionIndex = reminderIndex;
            }

            // Add appointment to calendar
            if (flag)
            {
                await ExportToExchangeCalendar(table, reminderIndex);
                await ret.SaveCache();
            }

            return ret;
        }

        private async Task<(bool TokenExpired, List<Class> Classes)> GetTimetable(DateTime from, DateTime to, string accountId, string token)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Get timetable
                    string _url = "http://portalapp.xjtlu.edu.cn/edu-app/api/userTimeTable/findStudentsTimeTablesById?userId={0}&userType=S&fromDate={1}&toDate={2}";
                    client.DefaultRequestHeaders.Add("Authorization", token);
                    HttpResponseMessage response = await client.GetAsync(string.Format(_url, accountId, WeekHelper.GetDateStr(from), WeekHelper.GetDateStr(to)));
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        return (true, null);
                    }
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return (false, null);
                    }
                    string responseStr = await response.Content.ReadAsStringAsync();

                    JObject rootObj = JObject.Parse(responseStr);
                    return (false, rootObj["data"]["timeTables"].ToObject<List<Class>>());
                }
            }
            catch
            {
                return (false, null);
            }
        }

        private async System.Threading.Tasks.Task ExportToExchangeCalendar(List<Class> classList, int reminderIndex)
        {
            CalendarFolder calendarFolder = null;
            for (int i = 0; i < 3; i++)
            {
                if (calendarFolder != null)
                {
                    break;
                }
                try
                {
                    calendarFolder = await CalendarFolder.Bind(Service, WellKnownFolderName.Calendar);
                }
                catch (ServiceRequestException) { }

                await System.Threading.Tasks.Task.Delay(50);
            }

            int attempts = 0;
            CalendarView calendarView = null;
            for (int i = 0; i < classList.Count;)
            {
                if (attempts >= 3) // give up after 3 times failure
                {
#if DEBUG
                    System.Console.WriteLine("DEBUG - Too many add attempts failed");
#endif
                    attempts = 0;
                    i++;
                    continue;
                }

                Class cls = classList[i];
                try
                {
                    // Validating exsisting appointment
                    if (!string.IsNullOrEmpty(cls.AppointmentId))
                    {
                        bool isExist = false;
                        calendarView = new CalendarView(cls.StartTime, cls.EndTime);
                        FindItemsResults<Appointment> results = await calendarFolder.FindAppointments(calendarView);
                        foreach (Appointment appoint in results)
                        {
                            if (string.Equals(cls.ModuleCode, appoint.Subject))
                            {
                                // TODO: Validate location and details info
#if DEBUG
                                System.Console.WriteLine("DEBUG - Skiping existing appointment {0}", cls.ModuleCode);
#endif
                                int minutes = GetReminderMinutes(reminderIndex);
                                if (minutes != appoint.ReminderMinutesBeforeStart)
                                {
                                    appoint.ReminderMinutesBeforeStart = minutes;
                                    await appoint.Update(ConflictResolutionMode.AlwaysOverwrite, SendInvitationsOrCancellationsMode.SendToNone);
                                }

                                cls.AppointmentId = appoint.Id.UniqueId;
                                break;
                            }
                        }

                        if (isExist)
                        {
                            i++;
                            continue;
                        }
                    }

                    Appointment appointment = new Appointment(Service);
                    appointment.IsAllDayEvent = false;

                    appointment.Subject = cls.ModuleCode;
                    appointment.Start = cls.StartTime;
                    appointment.End = cls.EndTime;
                    appointment.Location = cls.Location;
                    appointment.Sensitivity = Sensitivity.Private;

                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine(cls.ModuleTypeGroup);
                    builder.AppendLine(cls.StaffName);
                    builder.AppendLine(cls.ModuleType);
                    appointment.Body = new MessageBody(BodyType.Text, builder.ToString());

                    appointment.ReminderMinutesBeforeStart = GetReminderMinutes(reminderIndex);

                    await appointment.Save();
                    cls.AppointmentId = appointment.Id.UniqueId;

                    i++;
                    attempts = 0;
#if DEBUG
                    System.Console.WriteLine("DEBUG - Added appointment {0}", cls.ModuleCode);
#endif
                }
                catch (ServiceRequestException) 
                {
                    attempts++;
#if DEBUG
                    System.Console.WriteLine("DEBUG - Add appointment failed, attempt: ", attempts);
#endif
                }

                await System.Threading.Tasks.Task.Delay(50); // Avoid timeout execption (Adding appointment too fast)
            }
        }

        private int GetReminderMinutes(int reminderIndex)
        {
            switch (reminderIndex)
            {
                case 0:
                    return -1; //none
                case 1:
                    return 0;
                case 2:
                    return 5;
                case 3:
                    return 10;
                case 4:
                    return 15;
                case 5:
                    return 30;
                case 6:
                    return 60;
                case 7:
                    return 120;
            }
            return 15;
        }

        [Obsolete]
        private async System.Threading.Tasks.Task DeleteAppointmentFromExchange(ClassCache oldCache)
        {
            CalendarFolder calendarFolder = null;
            for (int i = 0; i < 3; i++)
            {
                if (calendarFolder != null)
                {
                    break;
                }
                try
                {
                    calendarFolder = await CalendarFolder.Bind(Service, WellKnownFolderName.Calendar);
                }
                catch (ServiceRequestException) { }

                await System.Threading.Tasks.Task.Delay(50);
            }

            int attempt = 0;

            // Delete appointment from calendar
            CalendarView calendarView = new CalendarView(oldCache.StartOfWeek, oldCache.GetEndDayOfWeek());

            for (int i = 0; i < oldCache.ClassList.Count;)
            {
                if (attempt >= 3)
                {
#if DEBUG
                    System.Console.WriteLine("DEBUG - Too many delete attempts failed");
#endif
                    attempt = 0;
                    i++;
                    continue;
                }

                try
                {
                    Class cls = oldCache.ClassList[i];
                    Appointment appoint = await Appointment.Bind(Service, cls.AppointmentId);
                    appoint?.Delete(DeleteMode.SoftDelete);

                    attempt = 0;
                    i++;
#if DEBUG
                    System.Console.WriteLine("DEBUG - Deleted appointment {0}", cls.ModuleCode);
#endif
                }
                catch (ServiceRequestException)
                {
                    attempt++;
#if DEBUG
                    System.Console.WriteLine("DEBUG - Delete failed, attempt: ", attempt);
#endif
                }

                await System.Threading.Tasks.Task.Delay(50); // Avoid timeout execption
            }

        }

        // To meet the Microsoft Store submission requirement.
        public static ClassCache LoadTestCache()
        {
            ClassCache ret = new ClassCache(WeekHelper.GetStartDayOfWeek(), new List<Class>(), new FileInfo("test.json"));
            DateTime baseTime = WeekHelper.GetStartDayOfWeek().AddDays(WeekHelper.Interval_StartOfNextWeek).AddHours(9);
            ret.ClassList.Add(new Class
            {
                ModuleCode = "EAP021",
                Location = "Foundation Building-FB321",
                StartTime = baseTime,
                EndTime = baseTime.AddHours(2)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "MTH013",
                Location = "Science Building Block B-SB222",
                StartTime = baseTime.AddDays(1).AddHours(6),
                EndTime = baseTime.AddDays(1).AddHours(8)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "CSE001",
                Location = "Science Building Block A-SA101",
                StartTime = baseTime.AddDays(2).AddHours(2),
                EndTime = baseTime.AddDays(2).AddHours(4)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "EAP021",
                Location = "Foundation Building-FB321",
                StartTime = baseTime.AddDays(3).AddHours(2),
                EndTime = baseTime.AddDays(3).AddHours(4)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "MTH013",
                Location = "Science Building Block B-SB222",
                StartTime = baseTime.AddDays(3).AddHours(6),
                EndTime = baseTime.AddDays(3).AddHours(8)
            });
            ret.ClassList.Add(new Class
            {
                ModuleCode = "MTH007",
                Location = "Science Building Block D-SD325",
                StartTime = baseTime.AddDays(4).AddHours(2),
                EndTime = baseTime.AddDays(4).AddHours(4)
            });
            return ret;
        }
    }
}
