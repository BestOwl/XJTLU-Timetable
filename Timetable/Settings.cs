using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Timetable.Core;
using Xamarin.Essentials;

namespace Timetable
{
    public class Settings
    {
        public static string Username
        {
            get => Preferences.Get("username", string.Empty);
            set => Preferences.Set("username", value);
        }

        public static string AccountId
        {
            get => Preferences.Get("id", string.Empty);
            set => Preferences.Set("id", value);
        }

        public static bool BackgroundUpdate
        {
            get => Preferences.Get("background-update", true);
            set => Preferences.Set("background-update", value);
        }

        public static int ReminderIndex
        {
            get => Preferences.Get("reminder-index", 1);
            set => Preferences.Set("reminder-index", value);
        }

        public static async Task<string> GetPassword()
        {
            return await SecureStorage.GetAsync("password");
        }

        public static async Task SetPassword(string password)
        {
            await SecureStorage.SetAsync("password", password);
        }

        public static async Task<string> GetTokenAsync()
        {
            return await SecureStorage.GetAsync("token");
        }

        public static async Task SetToken(string token)
        {
            await SecureStorage.SetAsync("token", token);
        }

        public static async void SaveAccount(XJTLUAccount acc)
        {
            await SetToken(acc.Token);
            Username = acc.Username;
            AccountId = acc.AccountId;
        }
    }
}
