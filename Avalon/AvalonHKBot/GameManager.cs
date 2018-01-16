﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace AvalonHKBot
{
    public class GameManager
    {
        private List<Avalon> _Games = new List<Avalon>();
        public List<Avalon> Games { get { return _Games; } set { _Games = Games; } }

        public GameManager()
        {

        }

    }

    public static class CustomMethods
    {
        public static Avalon GetGameByGuid(this GameManager gm, string Id)
        {
            return gm.GetGameByGuid(Guid.Parse(Id));
        }

        public static Avalon GetGameByGuid(this GameManager gm, Guid guid)
        {
            return gm.Games.FirstOrDefault(x => x.Id == guid);
        }

        public static Avalon GetGameByChatId(this GameManager gm, long chatId)
        {
            return gm.Games.FirstOrDefault(x => x.ChatId == chatId);
        }

        public static void AddGame(this GameManager gm, Avalon game)
        {
            gm.Games.Add(game);
        }

        #region Remove Game Methods
        public static void RemoveGame(this GameManager gm, Avalon game)
        {
            gm.Games.Remove(game);
        }

        public static void RemoveGame(this GameManager gm, string id)
        {
            gm.RemoveGame(gm.GetGameByGuid(id));
        }

        public static void RemoveGame(this GameManager gm, long chatId)
        {
            gm.RemoveGame(gm.GetGameByChatId(chatId));
        }
        #endregion

        #region Handle Messages
        public static void HandleMessage(this GameManager gm, Message msg)
        {
            var game = Bot.Gm.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                //
            }
            else
            {
                game.HandleMessage(msg);
            }
        }
        #endregion

        #region Handle Buttons
        public static void HandleQuery(this GameManager gm, CallbackQuery query, string[] args)
        {
            // args[0] = GameGuid
            // args[1] = playerId
            // args[2] = gameActionType
            // args[3] = cardId / playerId
            var game = Bot.Gm.GetGameByGuid(Guid.Parse(args[0]));
            if (game == null)
            {
                //
            }
            else
            {
                game.HandleQuery(query, args);
            }
        }
        #endregion
    }
}