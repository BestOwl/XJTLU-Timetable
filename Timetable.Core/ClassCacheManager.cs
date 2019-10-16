using Microsoft.Exchange.WebServices.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Timetable.Core
{
    public abstract class ClassCacheManager
    {
        public ExchangeService Service;
        public bool IsTestAccount = false;

        public ClassCacheManager()
        {
            Service = new ExchangeService();
            Service.Url = new Uri("https://mail.xjtlu.edu.cn/EWS/Exchange.asmx");
        }

        public string GetCacheFileName(string accountId, DateTime startOfWeek)
        {
            return string.Format(@"cache\{0}\timetable-{1}.json", accountId, WeekHelper.GetDateStr(startOfWeek));
        }

        public abstract Task<Stream> GetCacheFileAsync(string accountId, DateTime startOfWeek);

        /// <summary>
        /// Delete specific cache file
        /// </summary>
        /// <returns>Indicates whether the file was successfully deleted.</returns>
        public abstract Task<bool> DeleteCacheFileAsync(string accountId, DateTime startOfWeek);

        public async Task<ClassCache> LoadCache(DateTime startOfWeek, string accountId)
        {
            if (IsTestAccount)
            {
                return LoadTestCache();
            }

            using (Stream stream = await GetCacheFileAsync(accountId, startOfWeek))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    try
                    {
                        return JsonConvert.DeserializeObject<ClassCache>(await reader.ReadToEndAsync());
                    }
                    catch
                    {
                        await GetCacheFileAsync(accountId, startOfWeek);
                        return null;
                    }
                }
            }
        }

        public async Task<(bool success, List<ClassCache> classCaches)> UpdateTimetable(string accountId, string token, int reminderIndex)
        {
            DateTime startOfWeek = WeekHelper.GetStartDayOfWeek();
            DateTime endOfWeek = startOfWeek.AddDays(WeekHelper.Interval_EndOfWeek);
            DateTime startOfNextWeek = startOfWeek.AddDays(WeekHelper.Interval_StartOfNextWeek);
            DateTime endOfNextWeek = startOfWeek.AddDays(WeekHelper.Interval_EndOfNextWeek);

            List<Class> table1 = await GetTimetable(startOfWeek, endOfWeek, accountId, token);
            List<Class> table2 = await GetTimetable(startOfNextWeek, endOfNextWeek, accountId, token);
            if (table1 == null && table2 == null)
            {
                // Could not get timetable from portal, session timeout, need to re-login
                return (false, null);
            }

            List<ClassCache> ret = new List<ClassCache>();

            ClassCache cache = await LoadCache(startOfWeek, accountId);
            await UpdateTimetableInternal(table1, cache, startOfWeek, accountId, reminderIndex);
            ret.Add(cache);

            cache = await LoadCache(startOfNextWeek, accountId);
            await UpdateTimetableInternal(table2, cache, startOfNextWeek, accountId, reminderIndex);
            ret.Add(cache);

            return (true, ret);
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
                    // Delete appointment from calendar
                    CalendarFolder calendarFolder = await CalendarFolder.Bind(Service, WellKnownFolderName.Calendar);
                    CalendarView calendarView = new CalendarView(cache.StartOfWeek, cache.GetEndDayOfWeek());
                    FindItemsResults<Appointment> items = await calendarFolder.FindAppointments(calendarView);
                    foreach (Class cls in cache.ClassList)
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

                    cache.ClassList = table;
                }
            }
            else
            {
                cache = new ClassCache(startOfWeek, table);
            }

            // Add appointment to calendar
            await ExportToExchangeCalendar(table, reminderIndex);
            await cache.SaveCache(await GetCacheFileAsync(accountId, startOfWeek));
        }

        private async Task<List<Class>> GetTimetable(DateTime from, DateTime to, string accountId, string token)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Get timetable
                    string _url = "http://portalapp.xjtlu.edu.cn/edu-app/api/userTimeTable/findStudentsTimeTablesById?userId={0}&userType=S&fromDate={1}&toDate={2}";
                    client.DefaultRequestHeaders.Add("Authorization", token);
                    HttpResponseMessage response = await client.GetAsync(string.Format(_url, accountId, WeekHelper.GetDateStr(from), WeekHelper.GetDateStr(to)));
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return null;
                    }
                    string responseStr = await response.Content.ReadAsStringAsync();

                    JObject rootObj = JObject.Parse(responseStr);
                    return rootObj["data"]["timeTables"].ToObject<List<Class>>();
                }
            }
            catch
            {
                return null;
            }
        }

        private async System.Threading.Tasks.Task ExportToExchangeCalendar(List<Class> classList, int reminderIndex)
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

        // To meet the Microsoft Store submission requirement.
        public static ClassCache LoadTestCache()
        {
            ClassCache ret = new ClassCache(WeekHelper.GetStartDayOfWeek(), new List<Class>());
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
