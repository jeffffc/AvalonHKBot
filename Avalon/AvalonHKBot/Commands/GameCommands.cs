using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Command = AvalonHKBot.Attributes.Command;
using Telegram.Bot.Types;
using Database;

namespace AvalonHKBot
{
    public partial class Commands
    {
        [Command(Trigger = "startgame")]
        public static void StartGame(Message msg, string[] args)
        {
            Avalon game = Bot.Gm.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                if (Program.MaintMode)
                {
                    Bot.Send(msg.Chat.Id, GetTranslation("CantStartGameMaintenance", GetLanguage(msg.Chat.Id)));
                    return;
                }

                var botIsAdmin = BotMethods.IsBotAdmin(msg.Chat.Id);
                Bot.Gm.AddGame(new Avalon(msg.Chat.Id, msg.From, msg.Chat.Title, botIsAdmin));
            }
            else
            {
                Bot.Gm.HandleMessage(msg);
                // msg.Reply(GetTranslation("ExistingGame", GetLanguage(msg.Chat.Id)));
            }
        }

        [Command(Trigger = "test")]
        public static void Testing(Message msg, string[] args)
        {
            Avalon game = Bot.Gm.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
                Bot.Gm.HandleMessage(msg);
            }
        }

        [Command(Trigger = "join")]
        public static void JoinGame(Message msg, string[] args)
        {
            Avalon game = Bot.Gm.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
                Bot.Gm.HandleMessage(msg);
            }
        }

        [Command(Trigger = "flee")]
        public static void FleeGame(Message msg, string[] args)
        {
            Avalon game = Bot.Gm.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
                Bot.Gm.HandleMessage(msg);
            }
        }

        [Command(Trigger = "forcestart")]
        public static void ForceStart(Message msg, string[] args)
        {
            Avalon game = Bot.Gm.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
                Bot.Gm.HandleMessage(msg);
            }
        }

        [Command(Trigger = "killgame", AdminOnly = true)]
        public static void KillGame(Message msg, string[] args)
        {
            Avalon game = Bot.Gm.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
                Bot.Gm.HandleMessage(msg);
            }
        }

        [Command(Trigger = "questhistory")]
        public static void QuestHistory(Message msg, string[] args)
        {
            Avalon game = Bot.Gm.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
                Bot.Gm.HandleMessage(msg);
            }
        }
    }
}
