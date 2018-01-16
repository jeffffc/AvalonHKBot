using AvalonHKBot.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvalonHKBot
{
    public class Constants
    {
        // Token from registry
        private static RegistryKey _key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\AvalonHKBot");
        public static string GetBotToken(string key)
        {
            return _key.GetValue(key, "").ToString();
        }
        private static string _logPath = @"C:\Logs\AvalonHKBot.log";
        public static string GetLogPath()
        {
            return Path.GetFullPath(_logPath);
        }
        private static string _languageDirectory = @"C:\AvalonLanguages";
        public static string GetLangDirectory()
        {
            return Path.GetFullPath(_languageDirectory);
        }
        public static long LogGroupId = -1001276766474;
        public static int[] Dev = new int[] { 106665913, 415774316 };

        #region GameConstants
        public static int JoinTime = 90;
        public static int JoinTimeMax = 300;
        public static int MinPlayer = 5;
        public static int MaxPlayer = 10;
        public static int KingTime = 90;
        public static int ApproveMissionTime = 30;
        public static int MissionTime = 25;
        public static int KillMerlinTime = 90;
        public static int LakeTime = 30;

        #endregion

        public static Dictionary<int, List<List<AvalonRole>>> AvalonRoleSet = new Dictionary<int, List<List<AvalonRole>>>()
        {
            {
                5, new List<List<AvalonRole>>()
                {
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Mordred, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Auditor, AvalonRole.Mordred, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Ninja, AvalonRole.Assassin }
                }
            },
            {
                6, new List<List<AvalonRole>>()
                {
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Knight, AvalonRole.Mordred, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Auditor, AvalonRole.Mordred, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Auditor, AvalonRole.Mordred, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Ninja, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Ninja, AvalonRole.Assassin },
                }
            },
            {
                7, new List<List<AvalonRole>>()
                {
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Knight, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Morgana, AvalonRole.Assassin },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Morgana, AvalonRole.Assassin },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Ninja, AvalonRole.Assassin },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Agent, AvalonRole.Mordred, AvalonRole.Morgana, AvalonRole.Assassin }
                }
            },
            {
                8, new List<List<AvalonRole>>()
                {
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Agent, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Knight, AvalonRole.Knight, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Oberon, AvalonRole.Assassin },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Oberon, AvalonRole.Assassin }
                }
            },
            {
                9, new List<List<AvalonRole>>()
                {
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Auditor, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Auditor, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Oberon, AvalonRole.Morgause },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Auditor, AvalonRole.Knight, AvalonRole.Ninja, AvalonRole.Mordred, AvalonRole.Morgause }
                }
            },
            {
                10, new List<List<AvalonRole>>()
                {
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Witch, AvalonRole.Morgause, AvalonRole.Oberon },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Knight, AvalonRole.Auditor, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Witch, AvalonRole.Morgause, AvalonRole.Oberon },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgause, AvalonRole.Oberon },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgause, AvalonRole.Oberon },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Knight, AvalonRole.Knight, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgana, AvalonRole.Assassin },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Auditor, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgana, AvalonRole.Assassin },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Knight, AvalonRole.Servant, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgana, AvalonRole.Assassin },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Auditor, AvalonRole.Servant, AvalonRole.Ninja, AvalonRole.Morgana, AvalonRole.Assassin, AvalonRole.Oberon },
                    new List<AvalonRole>() {AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Agent, AvalonRole.Knight, AvalonRole.Ninja, AvalonRole.Mordred, AvalonRole.Witch, AvalonRole.Morgause }
                }
            }
        };

        public static Dictionary<int, int[]> MissionPlayerCount = new Dictionary<int, int[]>()
        {
            { 5, new int[] { 2, 3, 2, 3, 3} },
            { 6, new int[] { 2, 3, 4, 3, 4} },
            { 7, new int[] { 2, 3, 3, 4, 4} },
            { 8, new int[] { 3, 4, 4, 5, 5} },
            { 9, new int[] { 3, 4, 4, 5, 5} },
            { 10, new int[] { 3, 4, 4, 5, 5} }
        };
    }
}