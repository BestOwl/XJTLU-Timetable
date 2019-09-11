using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XJTLU_Timetable_UWP
{
    public class XJTLUAccount
    {
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }

        // In order to meet the Microsoft Store tester requirement
        public bool IsTestAccount()
        {
            if (string.Equals(Username, "test.testXJTLU"))
            {
                if (string.Equals(Password, "test123456XJTLU"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
