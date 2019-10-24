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

        public async Task<List<Class>> LoadClassListFromCache(DateTime startOfWeek, string accountId, int weekCount = 4)
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

        public async Task<(bool TokenExpired, List<ClassCache> classCaches)> UpdateTimetable(string accountId, string token, int reminderIndex, int weekCount)
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
                    await UpdateTimetableInternal(result.Classes, cache, startOfWeek, accountId, reminderIndex);
                }
                caches.Add(cache);

                startOfWeek = startOfWeek.AddDays(WeekHelper.Interval_StartOfNextWeek);
                endOfWeek = endOfWeek.AddDays(WeekHelper.Interval_StartOfNextWeek);
            }

            return (false, caches);
        }

        private async System.Threading.Tasks.Task UpdateTimetableInternal(List<Class> table, ClassCache cache, DateTime startOfWeek, string accountId, int reminderIndex)
        {
            if (cache != null)
            {
                if (cache.IsLatestCache(table, reminderIndex))
                {
                    return;
                }
                else
                {
                    if (Service.Credentials != null)
                    {
                        // Delete appointment from calendar
                        await DeleteAppointmentFromExchange(cache);

                        cache.ClassList = table;
                        cache.ReminderSelectionIndex = reminderIndex;
                    }
                }
            }
            else
            {
                cache = new ClassCache(startOfWeek, table,  new FileInfo(GetCacheFilePath(accountId, startOfWeek)));
                cache.ReminderSelectionIndex = reminderIndex;
            }

            // Add appointment to calendar
            if (Service.Credentials != null)
            {
                await ExportToExchangeCalendar(table, reminderIndex);
            }
            await cache.SaveCache();
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
            try
            {
                // TODO: Validate exsisting appointment
                foreach (Class cls in classList)
                {
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

                    switch (reminderIndex)
                    {
                        case 0:
                            appointment.IsReminderSet = false;
                            break;
                        case 1:
                            appointment.ReminderMinutesBeforeStart = 0;
                            break;
                        case 2:
                            appointment.ReminderMinutesBeforeStart = 5;
                            break;
                        case 3:
                            appointment.ReminderMinutesBeforeStart = 10;
                            break;
                        case 4:
                            appointment.ReminderMinutesBeforeStart = 15;
                            break;
                        case 5:
                            appointment.ReminderMinutesBeforeStart = 30;
                            break;
                        case 6:
                            appointment.ReminderMinutesBeforeStart = 60;
                            break;
                        case 7:
                            appointment.ReminderMinutesBeforeStart = 120;
                            break;
                    }

                    // TODO: Retry after saving failed (networking issue)
                    await appointment.Save();
                    cls.AppointmentId = appointment.Id.UniqueId;
                }
            }
            catch (ServiceRequestException) { }
        }

        private async System.Threading.Tasks.Task DeleteAppointmentFromExchange(ClassCache oldCache)
        {
            try
            {
                // Delete appointment from calendar
                CalendarFolder calendarFolder = await CalendarFolder.Bind(Service, WellKnownFolderName.Calendar);
                CalendarView calendarView = new CalendarView(oldCache.StartOfWeek, oldCache.GetEndDayOfWeek());
                FindItemsResults<Appointment> items = await calendarFolder.FindAppointments(calendarView);
                foreach (Class cls in oldCache.ClassList)
                {
                    foreach (Appointment apt in items)
                    {
                        if (Equals(cls.AppointmentId, apt.Id.UniqueId))
                        {
                            await apt.Delete(DeleteMode.MoveToDeletedItems);
                            break;
                        }
                    }
                }
            }
            catch (ServiceRequestException) { }
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
