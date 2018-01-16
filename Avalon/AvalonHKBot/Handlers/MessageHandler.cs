﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using AvalonHKBot;
using Database;

namespace AvalonHKBot.Handlers
{
    partial class Handler
    {
        public static void HandleMessage(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.TextMessage:
                    string text = msg.Text;
                    string[] args = text.Contains(' ')
                                    ? new[] { text.Split(' ')[0].ToLower(), text.Remove(0, text.IndexOf(' ') + 1) }
                                    : new[] { text.ToLower(), null };
                    if (args[0].EndsWith('@' + Bot.Me.Username.ToLower()))
                        args[0] = args[0].Remove(args[0].Length - Bot.Me.Username.Length - 1);
                    if (msg.Text.StartsWith("/"))
                    {
                        args[0] = args[0].Substring(1);
                        var cmd = Bot.Commands.FirstOrDefault(x => x.Trigger == args[0]);
                        if (cmd != null)
                        {
                            if (new[] { ChatType.Supergroup, ChatType.Group }.Contains(msg.Chat.Type))
                            {
                                using (var db = new AvalonDb())
                                {
                                    var DbGroup = db.Groups.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
                                    if (DbGroup == null)
                                    {
                                        DbGroup = Helper.MakeDefaultGroup(msg.Chat);
                                        db.Groups.Add(DbGroup);
                                        db.SaveChanges();
                                    }
                                }
                            }
                            if (cmd.GroupOnly && !new[] { ChatType.Supergroup, ChatType.Group }.Contains(msg.Chat.Type))
                            {
                                msg.Reply("This command can only be used in groups!");
                                return;
                            }

                            if (cmd.AdminOnly && !Helper.IsGroupAdmin(msg))
                            {
                                msg.Reply("You aren't a group admin!");
                                return;
                            }

                            if (cmd.GlobalAdminOnly && !Helper.IsGlobalAdmin(msg.From.Id) && !Constants.Dev.Contains(msg.From.Id))
                            {
                                msg.Reply("You aren't a global admin!");
                                return;
                            }

                            if (cmd.DevOnly && !Constants.Dev.Contains(msg.From.Id))
                            {
                                msg.Reply("You aren't a bot dev!");
                                return;
                            }

                            cmd.Method.Invoke(msg, args);
                            return;
                        }
                    }
                    if (msg.Chat.Type == ChatType.Supergroup && msg.Text.StartsWith("/startgame"))
                    {
                        // nothing
                    }
                    else if (msg.Text.StartsWith("/chatid"))
                    {
                        msg.Reply($"{msg.Chat.Id}");
                    }
                    else if (msg.Text.StartsWith("/join"))
                    {
                        var g = Bot.Gm.GetGameByChatId(msg.Chat.Id);
                    }
                    break;
                case MessageType.ServiceMessage:
                    //
                    break;
            }
        }
    }
}
