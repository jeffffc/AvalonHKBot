﻿using AvalonHKBot.Models;
using Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AvalonHKBot
{
    public static class Helper
    {
        public static void LogError(this Exception e)
        {
#if DEBUG
            /*
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("================================");
            Console.WriteLine($"Message: {e.Message}");
            Console.WriteLine($"Source: {e.Source}");
            */
            //Exception err;
            // string m  = $"Message: <code>{e.Message}</code>\nSource: <code>{e.Source}</code>\nStackTrace:\n{e.StackTrace}\n";
            /*err = e.InnerException;
            while (err != null)
            {
                m += $"{err.StackTrace}\n";
                err = err.InnerException;
            }*/
            /*
            Console.WriteLine($"StackTrace:\n{m}");
            Console.WriteLine("================================");
            Console.ResetColor();
            */
            string m = "Error occured." + Environment.NewLine + Environment.NewLine;
            var trace = $"<code>{e.StackTrace}</code>";
            do
            {
                m += $"<code>{e.Message}</code>" + Environment.NewLine + Environment.NewLine;
                e = e.InnerException;
            }
            while (e != null);

            m += trace;
            if (m.Contains("message is not modified"))
                return;

            Bot.Send(Constants.LogGroupId, m);
#else
            using (var sw = new StreamWriter(Constants.GetLogPath(), true))
            {
                sw.WriteLine("vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv");
                sw.WriteLine(DateTime.UtcNow);
                sw.WriteLine("================================");
                sw.WriteLine($"Message: {e.Message}");
                sw.WriteLine($"Source: {e.Source}");
                Exception err;
                string m = $"{e.StackTrace}\n";
                while (e.InnerException != null)
                {
                    err = e.InnerException;
                    m += $"{err.StackTrace}\n";
                }
                sw.WriteLine($"StackTrace:\n{m}");
                sw.WriteLine("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
            }
#endif
        }

        public static List<T> Shuffle<T>(this List<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        public static int RandomNum(int size)
        {
            Random rnd = new Random();
            return rnd.Next(0, size);
        }

        public static string ToBold(this object str)
        {
            if (str == null)
                return null;
            return $"<b>{str.ToString().FormatHTML()}</b>";
        }

        public static string ToItalic(this object str)
        {
            if (str == null)
                return null;
            return $"<i>{str.ToString().FormatHTML()}</i>";
        }

        public static string FormatHTML(this string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        public static string GetName(this AvalonPlayer player)
        {
            var name = player.Name;
            if (!String.IsNullOrEmpty(player.Username))
                return $"<a href=\"https://telegram.me/{player.Username}\">{name.FormatHTML()}</a>";
            return name.ToBold();
        }

        public static string GetName(this User player)
        {
            var name = player.FirstName;
            if (!String.IsNullOrEmpty(player.Username))
                return $"<a href=\"https://telegram.me/{player.Username}\">{name.FormatHTML()}</a>";
            return name.ToBold();
        }

        public static bool IsGroupAdmin(Message msg, bool IgnoreDev = false)
        {
            if (msg.Chat.Type == ChatType.Private) return false;
            if (msg.Chat.Type == ChatType.Channel) return false;
            return IsGroupAdmin(msg.From.Id, msg.Chat.Id, IgnoreDev);
        }

        public static bool IsGroupAdmin(int userid, long chatid, bool IgnoreDev = false)
        {
            if (Constants.Dev.Contains(userid) && !IgnoreDev) return true;

            var admins = Bot.Api.GetChatAdministratorsAsync(chatid).Result;
            if (admins.Any(x => x.User.Id == userid)) return true;
            return false;
        }

        public static bool IsGlobalAdmin(int id)
        {
            using (var db = new AvalonDb())
            {
                return db.Admins.Any(x => x.TelegramId == id);
            }
        }


        public static Group MakeDefaultGroup(Chat chat)
        {
            return new Group
            {
                GroupId = chat.Id,
                Name = chat.Title,
                Language = "English",
                CreatedBy = "Command",
                CreatedTime = DateTime.UtcNow,
                UserName = chat.Username,
                GroupLink = chat.Username == "" ? $"https://telegram.me/{chat.Username}" : null
            };
        }

        public static Dictionary<string, XDocument> ReadLanguageFiles()
        {
            var files = Directory.GetFiles(Constants.GetLangDirectory());
            var langs = new Dictionary<string, XDocument>();
            try
            {
                foreach (var file in files)
                {
                    var lang = Path.GetFileNameWithoutExtension(file);
                    XDocument doc = XDocument.Load(file);
                    langs.Add(lang, doc);
                }
            }
            catch { }
            return langs;
        }

        public static XDocument ReadEnglish()
        {
            return XDocument.Load(Path.Combine(Constants.GetLangDirectory(), "English.xml"));
        }
    }

    public static class EnumerableExtension
    {
        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.PickRandom(1).Single();
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }
    }
}
