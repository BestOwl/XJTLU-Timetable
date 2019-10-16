using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Timetable.Core
{
    public class Class
    {
        [JsonProperty("staffName")]
        public string StaffName { get; set; }
        [JsonProperty("moduleType")]
        public string ModuleType { get; set; }
        [JsonProperty("moduleCode")]
        public string ModuleCode { get; set; }
        [JsonProperty("room")]
        public string Room { get; set; }
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }
        [JsonProperty("staffID")]
        public string StaffId { get; set; }
        [JsonProperty("location")]
        public string Location { get; set; }
        [JsonProperty("moduleTypeGroup")]
        public string ModuleTypeGroup { get; set; }
        [JsonProperty("appointmentId")]
        public string AppointmentId { get; set; }

        [JsonIgnore]
        public string TimeStringDisplay
        {
            get => "Time: " + StartTime.ToString("yyyy-MM-dd  HH:mm") + " - " + EndTime.ToString("HH:mm");
        }

        public bool IsSameClass(Class obj)
        {
            if (!string.Equals(ModuleCode, obj.ModuleCode))
            {
                return false;
            }
            if (!string.Equals(ModuleTypeGroup, obj.ModuleTypeGroup))
            {
                return false;
            }
            if (!string.Equals(Location, obj.Location))
            {
                return false;
            }
            if (!string.Equals(StaffId, obj.StaffId))
            {
                return false;
            }
            if (!string.Equals(ModuleType, obj.ModuleType))
            {
                return false;
            }
            if (StartTime != obj.StartTime)
            {
                return false;
            }
            if (EndTime != obj.EndTime)
            {
                return false;
            }

            return true;
        }
    }

}
