using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace AvalonHKBot.Models
{
    public class AvalonPlayer
    {
        public int TelegramUserId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public int Id { get; set; }

        public AvalonRole Role { get; set; }
        public AvalonTeam Team { get; set; }
        public bool Ready { get; set; } = false;

        public int Choice1 { get; set; } = 0;
        public int Choice2 { get; set; } = 0;
        public int Choice3 { get; set; } = 0;
        public int Choice4 { get; set; } = 0;
        public int Choice5 { get; set; } = 0;
        public bool? Approve { get; set; } = null;

        public QuestionAsked CurrentQuestion { get; set; }
        public bool ReAnswer { get; set; } = false;

        public bool NinjaUsed { get; set; } = false;
        public bool? Success { get; set; } = null;
        public bool? Fail { get; set; } = null;
        public bool? FailTwice { get; set; } = null;
        public bool Lake { get; set; } = false;
        public int LakeChoice { get; set; } = 0;
        public bool? LakeDeclareChoice { get; set; } = null;
    }

    public class QuestionAsked
    {
        public QuestionType Type { get; set; }
        public int MessageId { get; set; } = 0;
    }

    public enum QuestionType
    {
        King, ApproveMission, DoMission, KillMerlin, Lake, LakeDeclare
    }
}