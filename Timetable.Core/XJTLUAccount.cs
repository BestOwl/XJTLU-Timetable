using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Timetable.Core
{
    public class XJTLUAccount
    {
        private const string _AuthUri = "http://portalapp.xjtlu.edu.cn/edu-app/api/authentication/login";

        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonIgnore]
        public string Token;
        [JsonIgnore]
        public string AccountId;

        public bool IsLogined
        {
            get => string.IsNullOrEmpty(Token);
        }

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

        public async Task<LoginResult> Login()
        {
            LoginResult result = new LoginResult();

            if (string.IsNullOrEmpty(Username))
            {
                result.Result = LoginResult.ResultType.NoUserPass;
                return result;
            }
            if (string.IsNullOrEmpty(Password))
            {
                result.Result = LoginResult.ResultType.NoUserPass;
                return result;
            }

            try
            {
                string responseStr = "";

                using (HttpClient client = new HttpClient())
                {
                    // Login to portal app
                    string acc = JsonConvert.SerializeObject(this);
                    StringContent content = new StringContent(acc, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(_AuthUri, content);
                    responseStr = await response.Content.ReadAsStringAsync();
                }

                // Get token and student internal id
                JObject rootObj = JObject.Parse(responseStr);
                if (rootObj["success"].Type != JTokenType.Boolean || !rootObj["success"].ToObject<bool>())
                {
                    string resultStr = rootObj["data"].ToString();
                    if (!string.IsNullOrEmpty(resultStr))
                    {
                        result.ResultDescription = resultStr;
                    }
                    result.Result = LoginResult.ResultType.UNAUTH;
                    return result;
                }

                Token = rootObj["data"]["token"].ToString();
                AccountId = rootObj["data"]["userId"].ToString();
                result.Result = LoginResult.ResultType.Success;
                return result;
            }
            catch (HttpRequestException)
            {
                result.Result = LoginResult.ResultType.Unkonwn;
                return result;
            }
        }
    }

    public class LoginResult
    {
        public string ResultDescription;
        public ResultType Result;

        public enum ResultType
        {
            Success,
            UNAUTH,
            NoUserPass,
            Unkonwn
        }
    }
}
