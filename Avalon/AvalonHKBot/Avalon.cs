using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using AvalonHKBot.Models;
using System.Threading;
using Database;
using System.Diagnostics;
using System.IO;
using Telegram.Bot.Types.InlineKeyboardButtons;
using System.Xml.Linq;

namespace AvalonHKBot
{
    public class Avalon : IDisposable
    {
        public long ChatId;
        public string GroupName;
        public Group DbGroup;
        public bool BotIsGroupAdmin = false;
        public int OldPinnedMsgId = 0;
        public Message Pinned;
        public List<AvalonPlayer> Players = new List<AvalonPlayer>();
        public Queue<AvalonPlayer> PlayerQueue = new Queue<AvalonPlayer>();
        public AvalonPlayer Initiator;
        public Guid Id = Guid.NewGuid();
        public int JoinTime = Constants.JoinTime;
        public GamePhase Phase = GamePhase.Joining;
        private int _secondsToAdd = 0;
        public GameAction NowAction = GameAction.King;
        private int _playerList = 0;
        private bool _questHistoryChanged = false;
        public int QuestHistoryMsgId = 0;
        public bool LancelotEnabled = false;
        public bool LakeEnabled = false;
        public int NextLake = 0;
        public List<AvalonPlayer> AlreadyLake = new List<AvalonPlayer>();
        public Dictionary<AvalonPlayer, Tuple<AvalonPlayer, bool?>> LakeHistory = new Dictionary<AvalonPlayer, Tuple<AvalonPlayer, bool?>>();
        public List<AvalonPlayer> MissionToBeDoneBy = new List<AvalonPlayer>();
        public bool MissionApproved = false;
        public AvalonMission CurrentMission = new AvalonMission { MissionNum = 1, MissionDone = false, MissionSuccess = false };
        public List<AvalonMission> OldMissions = new List<AvalonMission>();
        public int KingId = 0;
        public QuestionAsked CurrentQuestion;
        public List<int> MissionApprovalChosen = new List<int>();
        public bool approvalMsgEdit = false;

        public Locale Locale;
        public string Language = "English";

        public AvalonPlayer Winner;
        public AvalonTeam WinningTeam;

        public Avalon(long chatId, User u, string groupName, bool botIsAdmin = false)
        {
            #region Creating New Game - Preparation
            using (var db = new AvalonDb())
            {
                ChatId = chatId;
                GroupName = groupName;
                DbGroup = db.Groups.FirstOrDefault(x => x.GroupId == ChatId);
                LoadLanguage(DbGroup.Language);
                if (DbGroup == null)
                    Bot.Gm.RemoveGame(this);
            }
            // something
            #endregion

            AddPlayer(u, true);
            // var Pinned = Bot.Send(chatId, GetTranslation("NewGame", GetName(u)));
            Pinned = Bot.Send(chatId, GeneratePinned(), GeneratePinnedMenu());

            BotIsGroupAdmin = botIsAdmin;
            if (BotIsGroupAdmin)
            {
                OldPinnedMsgId = BotMethods.GetCurrentPinnedMsgId(ChatId);
                if (OldPinnedMsgId != 0)
                    BotMethods.PinMessage(ChatId, Pinned.MessageId);
            }
            new Thread(GameTimer).Start();
        }

        #region Main methods

        private void GameTimer()
        {
#if DEBUG
            // Add 4 fake players
            /*
            new Task(() => {
                AddPlayer(new User { FirstName = "Adnim", Id = 250543046, IsBot = false });
                AddPlayer(new User { FirstName = "Mud9User", Id = 433942669, IsBot = false });
                AddPlayer(new User { FirstName = "Ian", Id = 267359519, IsBot = false });
                AddPlayer(new User { FirstName = "j j", Id = 415774316, IsBot = false });
            }).Start(); 
            */
#endif

            while (Phase != GamePhase.Ending)
            {
                for (var i = 0; i < JoinTime; i++)
                {
                    if (this.Phase == GamePhase.InGame)
                        break;
                    if (this.Phase == GamePhase.Ending)
                        return;

                    if (_secondsToAdd != 0)
                    {
                        i = Math.Max(i - _secondsToAdd, Constants.JoinTime - Constants.JoinTimeMax);
                        // Bot.Send(ChatId, GetTranslation("JoinTimeLeft", TimeSpan.FromSeconds(Constants.JoinTime - i).ToString(@"mm\:ss")));
                        _secondsToAdd = 0;
                    }
                    var specialTime = JoinTime - i;
                    if (new int[] { 10, 30, 60 }.Contains(specialTime))
                    {
                        Reply(Pinned.MessageId, GetTranslation("JoinTimeSpecialSeconds", specialTime));
                    }
                    if (Players.Count >= Constants.MinPlayer && Players.All(x => x.Ready == true))
                        break;
                    if (Players.Count == Constants.MaxPlayer)
                        break;
                    Thread.Sleep(1000);
                }
                Pinned.EditMarkup();
                do
                {
                    AvalonPlayer p = Players.FirstOrDefault(x => Players.Count(y => y.TelegramUserId == x.TelegramUserId) > 1);
                    if (p == null) break;
                    Players.Remove(p);
                }
                while (true);

                if (this.Players.Count >= Constants.MinPlayer)
                    this.Phase = GamePhase.InGame;
                if (this.Phase != GamePhase.InGame)
                {
                    /*
                    this.Phase = GamePhase.Ending;
                    Bot.Gm.RemoveGame(this);
                    Bot.Send(ChatId, "Game ended!");
                    */
                }
                else
                {
                    #region Ready to start game
                    if (Players.Count < Constants.MinPlayer)
                    {
                        Send(GetTranslation("GameEnded"));
                        return;
                    }

                    Send(GetTranslation("GameStart"));
                    PrepareGame(Players.Count());

                    #endregion

                    #region Start!
                    //FirstFinder();
                    
                    
                    while (NowAction != GameAction.Ending)
                    {
                        // _playerList = Send(GeneratePlayerList()).MessageId;
                        //if (LakeEnabled)
                            //Lake();
                        King();
                        //ApproveMission();
                        if (MissionApproved)
                            DoMission();
                        if (CurrentMission.MissionDone && OldMissions.Count(x => x.MissionSuccess == true) >= 2) // if 3 successes
                            break;
                        if (CurrentMission.MissionSuccess == false && OldMissions.Count(x => x.MissionSuccess == false) >= 2) // if 3 fails
                            break;
                        SwitchKing();
                        if (CurrentMission.MissionDone)
                        {
                            if (new int[] { 5, 8, 9, 10 }.Contains(Players.Count) && new int[] { 2, 3, 4 }.Contains(CurrentMission.MissionNum))
                                Lake();
                            SwitchMission();
                        }
                    }
                    
                    if (Phase == GamePhase.Ending)
                        break;
                    EndGame();
                    
                    #endregion
                    this.Phase = GamePhase.Ending;
                }
                this.Phase = GamePhase.Ending;
            }

            Bot.Gm.RemoveGame(this);
            Send(GetTranslation("GameEnded"));
            if (OldPinnedMsgId != 0 && BotIsGroupAdmin)
                BotMethods.PinMessage(ChatId, OldPinnedMsgId);
        }

        #endregion

        #region Preparation
        private void AddPlayer(User u, bool newGame = false)
        {
            if (Phase == GamePhase.InGame)
                return;
            var player = this.Players.FirstOrDefault(x => x.TelegramUserId == u.Id);
            if (player != null)
                return;

            /*
            player = this.Players.FirstOrDefault(x => x.Name.ToLower() == u.FirstName.ToLower());
            var accomp = GetTranslation("AccompliceAppendName");                     // Avoid joining with (Accomplice) in name
            if (player != null || u.FirstName.ToLower().Contains(accomp.ToLower()))  // Avoid 2 players having the same name
            {
                Send(GetTranslation("ChangeNameToJoin", u.GetName()));
                return;
            }
            */



            using (var db = new AvalonDb())
            {
                var DbPlayer = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                if (DbPlayer == null)
                {
                    DbPlayer = new Player
                    {
                        TelegramId = u.Id,
                        Name = u.FirstName,
                        Language = "English"
                    };
                    db.Players.Add(DbPlayer);
                    db.SaveChanges();
                }
                AvalonPlayer p = new AvalonPlayer
                {
                    Name = u.FirstName,
                    Id = DbPlayer.Id,
                    TelegramUserId = u.Id,
                    Username = u.Username
                };
                try
                {
                    Message ret;
                    try
                    {
                        ret = SendPM(p, GetTranslation("YouJoined", GroupName));
                    }
                    catch
                    {
                        Bot.Send(ChatId, GetTranslation("NotStartedBot", GetName(u)), GenerateStartMe());
                        return;
                    }
                }
                catch { }
                this.Players.Add(p);

            }
            if (!newGame)
            {
                _secondsToAdd += 15;
                Pinned.Edit(GeneratePinned(), GeneratePinnedMenu());
            }

            do
            {
                AvalonPlayer p = Players.FirstOrDefault(x => Players.Count(y => y.TelegramUserId == x.TelegramUserId) > 1);
                if (p == null) break;
                Players.Remove(p);
            }
            while (true);

            // Send(GetTranslation("JoinedGame", GetName(u)) + Environment.NewLine + GetTranslation("JoinInfo", Players.Count, 5, 10));
        }

        private void RemovePlayer(User user)
        {
            if (this.Phase != GamePhase.Joining) return;

            var player = this.Players.FirstOrDefault(x => x.TelegramUserId == user.Id);
            if (player == null)
                return;

            this.Players.Remove(player);

            do
            {
                AvalonPlayer p = Players.FirstOrDefault(x => Players.Count(y => y.TelegramUserId == x.TelegramUserId) > 1);
                if (p == null) break;
                Players.Remove(p);
            }
            while (true);

            // Send(GetTranslation("FledGame", user.GetName()) + Environment.NewLine + GetTranslation("JoinInfo", Players.Count, 3, 8));
            Pinned.Edit(GeneratePinned(), GeneratePinnedMenu());
        }

        public void PrepareGame(int NumOfPlayers)
        {
            var tempPlayerList = Players.Shuffle();
            PlayerQueue = new Queue<AvalonPlayer>(tempPlayerList);
            var goodRoles = new[] { AvalonRole.Merlin, AvalonRole.Percival, AvalonRole.Servant, AvalonRole.Knight, AvalonRole.Agent, AvalonRole.Auditor };
            var badRoles = new[] { AvalonRole.Assassin, AvalonRole.Morgana, AvalonRole.Mordred, AvalonRole.Oberon, AvalonRole.Morgause, AvalonRole.Witch, AvalonRole.Ninja };
            foreach (AvalonPlayer p in Players)
            {
                p.Choice1 = 0;
                p.Choice2 = 0;
                p.Choice3 = 0;
                p.Choice4 = 0;
            }
            var thisGameRoles = Constants.AvalonRoleSet[NumOfPlayers].ElementAt(Helper.RandomNum(Constants.AvalonRoleSet[NumOfPlayers].Count)).Shuffle().Shuffle();
            for (int i = 0; i < NumOfPlayers; i++)
            {
                var p = Players[i];
                p.Role = thisGameRoles[i];
                p.Team = goodRoles.Contains(p.Role) ? AvalonTeam.Good : AvalonTeam.Bad;
            }
            foreach (AvalonPlayer p in Players)
            { 
                var msg = "";
                var badTeammatesExceptOberon = Players.Where(x => x.Team == AvalonTeam.Bad && x.Role != AvalonRole.Oberon && x != p);
                switch (p.Role)
                {
                    case AvalonRole.Merlin:
                        msg = GetTranslation("MerlinPMInfo", GetTranslation(p.Role.ToString()),
                            Players.Where(x => x.Team == AvalonTeam.Bad && !new AvalonRole[] { AvalonRole.Ninja, AvalonRole.Mordred }.Contains(x.Role)).Select(
                                x => x.Name.FormatHTML()).Aggregate((x, y) => x + GetTranslation("And") + y));
                        break;
                    case AvalonRole.Percival:
                        var merlins = Players.Where(x => new AvalonRole[] { AvalonRole.Merlin, AvalonRole.Morgana, AvalonRole.Morgause }.Contains(x.Role));
                        msg = GetTranslation("PercivalPMInfo", GetTranslation(p.Role.ToString()),
                            merlins.Select(x => x.Name.FormatHTML()).Aggregate((x, y) => x + GetTranslation("And") + y));
                        break;
                    case AvalonRole.Servant:
                    case AvalonRole.Agent:
                    case AvalonRole.Auditor:
                        msg = GetTranslation($"{p.Role.ToString()}PMInfo", GetTranslation(p.Role.ToString()));
                        break;
                    case AvalonRole.Knight:
                        // later
                        msg = GetTranslation("KnightPMInfo", GetTranslation(p.Role.ToString()), "test", "test");
                        break;
                    case AvalonRole.Mordred:
                    case AvalonRole.Ninja:
                    case AvalonRole.Assassin:
                    case AvalonRole.Morgana:
                    case AvalonRole.Morgause:
                    case AvalonRole.Oberon:
                        msg = GetTranslation($"{p.Role.ToString()}PMInfo", GetTranslation(p.Role.ToString()), badTeammatesExceptOberon.Select(x => x.GetName()).Aggregate((x, y) => x+ GetTranslation("And") + y));
                        break;
                    case AvalonRole.Witch:
                        msg = GetTranslation("WitchPMInfo", GetTranslation(p.Role.ToString()), badTeammatesExceptOberon.Select(x => x.GetName()).Aggregate((x, y) => x + GetTranslation("And") + y),
                            thisGameRoles.Distinct().Select(x => x.ToString()).Aggregate((x, y) => x + GetTranslation("And") + y));
                        break;
                    
                }
                SendPM(p, msg);
            }
#if DEBUG
            Send(Players.Select(x => GetName(x) + " " + x.Role.ToString()).Aggregate((x, y) => x + Environment.NewLine + y));
#endif
            CurrentMission = GenerateMission(1);
        }

        public void King()
        {
            CleanAll();
            var mission = CurrentMission;
            var king = PlayerQueue.Peek();
            CleanChoice(king);
            var basemsg = GenerateMissionPlayerList(mission.MissionNum);
            var msg = basemsg + Environment.NewLine + Environment.NewLine + GenerateMissionQuestion(king.Name, mission.MissionNum);
            // announce mission
            Announce(msg);

            // send question to king
            SendMenu(king, GetTranslation("MissionStartKingChoosePM", 1, mission.NumOfPlayers), GenerateKingChoiceMenu(king), QuestionType.King);

            // let king choose
            for (int i = 0; i < Constants.KingTime; i++)
            {
                Thread.Sleep(1000);
                /*
                switch (mission.NumOfPlayers)
                {
                    case 2:
                        if (king.Choice1 != 0 && king.Choice2 != 0)
                            exitFor = true;
                        break;
                    case 3:
                        if (king.Choice1 != 0 && king.Choice2 != 0 && king.Choice3 != 0)
                            exitFor = true;
                        break;
                    case 4:
                        if (king.Choice1 != 0 && king.Choice2 != 0 && king.Choice3 != 0 && king.Choice4 != 0)
                            exitFor = true;
                        break;
                    case 5:
                        if (king.Choice1 != 0 && king.Choice2 != 0 && king.Choice3 != 0 && king.Choice4 != 0 && king.Choice5 != 0)
                            exitFor = true;
                        break;
                }
                if (exitFor)
                    break;
                    */
                if (king.CurrentQuestion == null)
                    break;
            }
            try
            {
                foreach (var p in MissionToBeDoneBy.Where(x => x.CurrentQuestion != null))
                {
                    try
                    {
                        if (p.CurrentQuestion.MessageId != 0)
                        {
                            Bot.Edit(p.Id, p.CurrentQuestion.MessageId, GetTranslation("TimesUpButton"));
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    p.CurrentQuestion = null;
                }
            }
            catch
            {
                // ignored
            }

            MissionToBeDoneBy = Players.Where(x => new int[] { king.Choice1, king.Choice2, king.Choice3, king.Choice4, king.Choice5 }.Contains(x.TelegramUserId)).ToList();

            while (MissionToBeDoneBy.Count < CurrentMission.NumOfPlayers)
            {
                MissionToBeDoneBy.Add(Players.Where(x => !MissionToBeDoneBy.Any(y => x.TelegramUserId == y.TelegramUserId)).PickRandom());
            }
            

            msg = basemsg += Environment.NewLine + Environment.NewLine + GetTranslation("MissionKingChosen", GetName(king),
                MissionToBeDoneBy.Select(x => x.GetName()).Aggregate((x, y) => x + GetTranslation("And") + y));
            msg += Environment.NewLine + Environment.NewLine + GetTranslation("ApproveMission", Constants.ApproveMissionTime);
            // announce mission
            Announce(msg);
            var sent = SendMenu(GetTranslation("ApproveReject"), GenerateApprovalMenu(), QuestionType.ApproveMission);

            for (int i = 0; i < Constants.ApproveMissionTime; i++)
            {
                Thread.Sleep(1000);
                if (Players.Count(x => x.Approve != null) == Players.Count)
                    break;
            }
            var approveCount = Players.Count(x => x.Approve == true);
            var rejectCount = Players.Count(x => x.Approve == false);
            var afkCount = Players.Count(x => x.Approve == null);
            var approvedBy = Players.Where(x => x.Approve == true).ToList();
            var rejectedBy = Players.Where(x => x.Approve == false).ToList();

            /*
            msg = GetTranslation("ApproveReject") + Environment.NewLine;
            msg += $"{GetTranslation("Approve")} ({approveCount}): {(approveCount > 0 ? approvedBy.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y) : "---")}" + Environment.NewLine;
            msg += $"{GetTranslation("Reject")} ({rejectCount}): {(rejectCount > 0 ? rejectedBy.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y) : "---")}" + Environment.NewLine;
            sent.Edit(msg);
            */
            sent.EditMarkup();
            sent.EditMarkup();

            //msg = GetTranslation("MissionTimesUp") + Environment.NewLine + Environment.NewLine;
            msg = basemsg += Environment.NewLine + Environment.NewLine;

            if (approveCount >= rejectCount)
            {
                msg += GetTranslation("AfterApprove") + Environment.NewLine + Environment.NewLine;
                MissionApproved = true;
            }
            else
                msg += GetTranslation("RejectChangeKing", king.GetName()) + Environment.NewLine + Environment.NewLine;

            msg += $"{GetTranslation("Approve")} ({approveCount}): {(approveCount > 0 ? approvedBy.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y) : "---")}" + Environment.NewLine;
            msg += $"{GetTranslation("Reject")} ({rejectCount}): {(rejectCount > 0 ? rejectedBy.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y) : "---")}" + Environment.NewLine;
            if (afkCount > 0)
                msg += $"{GetTranslation("AFK")} ({afkCount}): {Players.Where(x => x.Approve == null).Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y)}" + Environment.NewLine;
            // debug
            Announce(msg);
            CurrentMission.Attempts.Add(new AvalonMissionAttempt { Approved = MissionApproved, ApprovedBy = approvedBy, RejectedBy = rejectedBy });
            //NowAction = GameAction.Ending;
            //Phase = GamePhase.Ending;
            // debug
            foreach (var p in Players)
                CleanChoice(p);
            return;
        }

        public void DoMission()
        {
            NowAction = GameAction.DoMission;
            CurrentMission.MissionDoneKing = PlayerQueue.Peek();
            foreach (var p in MissionToBeDoneBy)
            {
                CleanChoice(p);
            }
            var goodGuys = MissionToBeDoneBy.Where(x => x.Team == AvalonTeam.Good);
            var badGuys = MissionToBeDoneBy.Where(x => x.Team == AvalonTeam.Bad);
            foreach (var p in goodGuys)
                SendPM(p, GetTranslation("DoMissionGood"));
            foreach (var p in badGuys)
                SendMenu(p, GetTranslation("DoMissionBad", CurrentMission.MissionNum), GenerateBadGuyMenu(p), QuestionType.DoMission);
            Send(GetTranslation("DoMissionNow", MissionToBeDoneBy.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y), Constants.MissionTime));
            for (int i = 0; i < Constants.MissionTime; i++)
            {
                Thread.Sleep(1000);
            }
            try
            {
                foreach (var p in badGuys.Where(x => x.CurrentQuestion != null))
                {
                    try
                    {
                        if (p.CurrentQuestion.MessageId != 0)
                        {
                            Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("TimesUpButton"));
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    p.CurrentQuestion = null;
                }
            }
            catch
            {
                // ignored
            }
            var msg  = "";
            if (MissionToBeDoneBy.Any(x => x.Fail == true || x.FailTwice == true))
            {
                msg = GetTranslation("MissionFailed", MissionToBeDoneBy.Count(x => x.Fail == true) + MissionToBeDoneBy.Count(x => x.FailTwice == true) * 2);
                CurrentMission.MissionSuccess = false;
                if (CurrentMission.MissionNum == 4)
                {
                    switch (Players.Count)
                    {
                        case 5:
                        case 6:
                            break;
                        default:
                            if ((MissionToBeDoneBy.Count(x => x.Fail == true) + MissionToBeDoneBy.Count(x => x.FailTwice == true) * 2) >= 2)
                            {
                                msg = GetTranslation("MissionSucceeded");
                                CurrentMission.MissionSuccess = true;
                            }
                            break;
                    }
                }
            }
            else
            {
                msg = GetTranslation("MissionSucceeded");
                CurrentMission.MissionSuccess = true;
            }
            CurrentMission.MissionDone = true;
            CurrentMission.MissionDoneBy = MissionToBeDoneBy;
            Send(msg);
            return;
        }


        public void Lake()
        {
            AvalonPlayer p;
            if (CurrentMission.MissionNum == 2)
            {
                // bot assign lake
                p = PlayerQueue.ToList()[1];
                p.Lake = true;
            }
            else
            {
                // use old lake
                if (NextLake == 0)
                    p = PlayerQueue.ToList()[1];
                else
                    p = Players.FirstOrDefault(x => x.TelegramUserId == NextLake);
                p.Lake = true;
            }

            Send(GetTranslation("LakeCanChoose", p.GetName(), Constants.LakeTime));

            SendMenu(p, GetTranslation("LakeChoose"), GenerateLakeMenu(p), QuestionType.Lake);

            for (int i = 0; i < Constants.LakeTime; i++)
            {
                Thread.Sleep(1000);
                if (p.CurrentQuestion == null && p.LakeChoice != 0)
                    break;
            }

            try
            {
                if (p.CurrentQuestion.MessageId != 0)
                {
                    Bot.Edit(p.Id, p.CurrentQuestion.MessageId, GetTranslation("LakePMTimesUp"));
                }
            }
            catch
            {
                // ignored
            }
            p.CurrentQuestion = null;
            if (p.LakeChoice == 0)
            {
                Send(GetTranslation("LakeTimesUp"));
                LakeHistory.Add(p, null);
                _questHistoryChanged = true;
                return;
            }
            NextLake = p.LakeChoice;
            var next = Players.FirstOrDefault(x => x.TelegramUserId == NextLake);
            AlreadyLake.Add(next);
            var resultmsg = GetTranslation("LakeResultChoose", next.GetName(), (next.Team == AvalonTeam.Good ? GetTranslation("GoodWord") : GetTranslation("BadWord")));
            SendMenu(p, resultmsg, GenerateLakeDeclareMenu(p), QuestionType.LakeDeclare);

            for (int i = 0; i < Constants.LakeTime; i++)
            {
                Thread.Sleep(1000);
                if (p.CurrentQuestion == null && p.LakeDeclareChoice != null)
                    break;
            }

            try
            {
                if (p.CurrentQuestion.MessageId != 0)
                {
                    Bot.Edit(p.Id, p.CurrentQuestion.MessageId, GetTranslation("TimesUpButton"));
                }
            }
            catch
            {
                // ignored
            }
            p.CurrentQuestion = null;
            if (p.LakeDeclareChoice == null)
            {
                Send(GetTranslation("LakeResultNoChoose", p.GetName(), next.GetName()));
                LakeHistory.Add(p, null);
                _questHistoryChanged = true;
                return;
            }
            LakeHistory.Add(p, Tuple.Create(next, p.LakeDeclareChoice));
            _questHistoryChanged = true;
            var result2 = GetTranslation("LakeFinalResult", p.GetName(), next.GetName(), (p.LakeDeclareChoice == true ? GetTranslation("GoodWord") : GetTranslation("BadWord")));
            Send(result2);

            p.Lake = false;
            p.LakeChoice = 0;
        }

        public void SwitchKing()
        {
            var p = PlayerQueue.Dequeue();
            CleanChoice(p);
            PlayerQueue.Enqueue(p);
            NowAction = GameAction.King;
        }

        public void CleanChoice(AvalonPlayer p)
        {
            p.Choice1 = 0;
            p.Choice2 = 0;
            p.Choice3 = 0;
            p.Choice4 = 0;
            p.Choice5 = 0;
            p.Approve = null;
            p.LakeChoice = 0;
            p.LakeDeclareChoice = null;
        }

        public void SwitchMission()
        {
            OldMissions.Add(CurrentMission);
            _questHistoryChanged = true;
            CurrentMission = GenerateMission(OldMissions.Count + 1);
        }

        public void CleanAll()
        {
            MissionApproved = false;
            MissionApprovalChosen.Clear();
        }



        public InlineKeyboardMarkup GenerateLakeMenu(AvalonPlayer p)
        {
            var buttons = new List<Tuple<string, string>>();
            var plist = Players.ToList().Where(x => x.TelegramUserId != p.TelegramUserId && !AlreadyLake.Contains(x));
            foreach (AvalonPlayer pp in plist)
            {
                buttons.Add(new Tuple<string, string>(pp.Name, $"{this.Id}|{p.TelegramUserId}|lake|{pp.TelegramUserId}"));
            }
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < buttons.Count; i++)
            {
                row.Clear();
                row.Add(new InlineKeyboardCallbackButton(buttons[i].Item1, buttons[i].Item2));
                rows.Add(row.ToArray());
            }
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        public InlineKeyboardMarkup GenerateLakeDeclareMenu(AvalonPlayer p)
        {
            var buttons = new List<Tuple<string, string>>();
            buttons.Add(new Tuple<string, string>(GetTranslation("GoodWord"), $"{this.Id}|{p.TelegramUserId}|lakedeclare|good"));
            buttons.Add(new Tuple<string, string>(GetTranslation("BadWord"), $"{this.Id}|{p.TelegramUserId}|lakedeclare|bad"));

            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < buttons.Count; i++)
            {
                row.Clear();
                row.Add(new InlineKeyboardCallbackButton(buttons[i].Item1, buttons[i].Item2));
                rows.Add(row.ToArray());
            }
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        public InlineKeyboardMarkup GenerateKingChoiceMenu(AvalonPlayer p)
        {
            var buttons = new List<Tuple<string, string>>();
            var plist = Players.ToList().Where(x => !new int[] { p.Choice1, p.Choice2, p.Choice3, p.Choice4, p.Choice5 }.Contains(x.TelegramUserId));
            foreach (AvalonPlayer pp in plist)
            {
                buttons.Add(new Tuple<string, string>(pp.Name, $"{this.Id}|{p.TelegramUserId}|king|{pp.TelegramUserId}"));
            }
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < buttons.Count; i++)
            {
                row.Clear();
                row.Add(new InlineKeyboardCallbackButton(buttons[i].Item1, buttons[i].Item2));
                rows.Add(row.ToArray());
            }
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        public InlineKeyboardMarkup GenerateKillMerlinMenu(AvalonPlayer p)
        {
            var buttons = new List<Tuple<string, string>>();
            var plist = Players.ToList().Where(x => x.Team == AvalonTeam.Good);
            foreach (AvalonPlayer pp in plist)
            {
                buttons.Add(new Tuple<string, string>(pp.Name, $"{this.Id}|{p.TelegramUserId}|kill|{pp.TelegramUserId}"));
            }
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < buttons.Count; i++)
            {
                row.Clear();
                row.Add(new InlineKeyboardCallbackButton(buttons[i].Item1, buttons[i].Item2));
                rows.Add(row.ToArray());
            }
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        public InlineKeyboardMarkup GenerateApprovalMenu()
        {
            var buttons = new List<InlineKeyboardButton>();
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("Approve"), $"{this.Id}|approval|approve"));
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("Reject"), $"{this.Id}|approval|reject"));
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    twoMenu.Add(new[] { buttons[i] });
                }
                else
                    twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }
            return new InlineKeyboardMarkup(twoMenu.ToArray());
        }

        public InlineKeyboardMarkup GenerateBadGuyMenu(AvalonPlayer p)
        {
            var buttons = new List<Tuple<string, string>>();
            buttons.Add(new Tuple<string, string>(GetTranslation("DoMissionBadChoiceSuccess"), $"{this.Id}|{p.TelegramUserId}|domission|success"));
            buttons.Add(new Tuple<string, string>(GetTranslation("DoMissionBadChoiceFail"), $"{this.Id}|{p.TelegramUserId}|domission|fail"));
            if (p.Role == AvalonRole.Ninja && !p.NinjaUsed)
                buttons.Add(new Tuple<string, string>(GetTranslation("DoMissionBadChoiceFailTwice"), $"{this.Id}|{p.TelegramUserId}|domission|failtwice"));

            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < buttons.Count; i++)
            {
                row.Clear();
                row.Add(new InlineKeyboardCallbackButton(buttons[i].Item1, buttons[i].Item2));
                rows.Add(row.ToArray());
            }
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        public AvalonMission GenerateMission(int missionNum)
        {
            return new AvalonMission { MissionNum = missionNum, NumOfPlayers = Constants.MissionPlayerCount[Players.Count][missionNum - 1] };
        }

        public string GenerateMissionPlayerList(int currentMissionNum)
        {
            string msg = "";
            var oldCount = OldMissions.Count;
            var playerCount = Players.Count;
            for (int i = 1; i <= 5; i++)
            {
                if (i <= oldCount)
                    msg += $"<i>{GetTranslation("MissionNotCurrent", i, Constants.MissionPlayerCount[playerCount][i - 1], GetTranslation(OldMissions[i - 1].MissionSuccess == true ? "Success" : "Failed"))}</i>" + Environment.NewLine;
                else if (i == currentMissionNum)
                    msg += $"<b>{GetTranslation("MissionCurrent", i, Constants.MissionPlayerCount[playerCount][i - 1])}</b>" + Environment.NewLine;
                else
                    msg += $"{GetTranslation("MissionNotCurrent", i, Constants.MissionPlayerCount[playerCount][i - 1], GetTranslation("Unknown"))}" + Environment.NewLine;
            }
            if (CurrentMission.Attempts.Count > 0)
                msg += Environment.NewLine + GetTranslation("ThisMissionRejectedCount", string.Concat(Enumerable.Repeat(GetTranslation("RejectedSymbol"), CurrentMission.Attempts.Count(x => x.Approved == false)))) + Environment.NewLine;
            msg += Environment.NewLine + GeneratePlayerList();
            return msg;
        }

        public string GenerateMissionPlayerList(bool endGame = false)
        {
            string msg = "";
            var oldCount = OldMissions.Count;
            var playerCount = Players.Count;
            for (int i = 1; i <= 5; i++)
            {
                if (i <= oldCount)
                    msg += $"{GetTranslation("MissionNotCurrent", i, Constants.MissionPlayerCount[playerCount][i - 1], GetTranslation(OldMissions[i - 1].MissionSuccess == true ? "Success" : "Failed"))}</i>" + Environment.NewLine;
                else
                    msg += $"{GetTranslation("MissionNotCurrent", i, Constants.MissionPlayerCount[playerCount][i - 1], "")}" + Environment.NewLine;
            }
            if (CurrentMission.Attempts.Count > 0)
                msg += Environment.NewLine + GetTranslation("ThisMissionRejectedCount", string.Concat(Enumerable.Repeat(GetTranslation("RejectedSymbol"), CurrentMission.Attempts.Count(x => x.Approved == false)))) + Environment.NewLine;
            msg += Environment.NewLine + GeneratePlayerList(endGame);
            return msg;
        }

        public string GenerateMissionQuestion(string name, int missionNum)
        {
            var num = Constants.MissionPlayerCount[Players.Count][missionNum - 1];
            return GetTranslation("MissionStartKingChoose", name, num, Constants.KingTime);
        }

        public string GenerateQuestHistory()
        {
            string msg = GetTranslation("QuestHistoryTitle") + Environment.NewLine;
            foreach (var mission in OldMissions)
            {
                msg += GetTranslation("QuestHistoryEach", mission.MissionNum, mission.MissionSuccess == true ? GetTranslation("Success") : GetTranslation("Failed"), mission.MissionDoneKing.GetName(),
                    mission.MissionDoneBy.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y));
                msg += Environment.NewLine;
                var lastAttempt = mission.Attempts.Last();
                var app = lastAttempt.ApprovedBy;
                var rej = lastAttempt.RejectedBy;
                msg += GetTranslation("Approve") + ": " + ((app.Count > 0) ? app.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y) : "");
                msg += Environment.NewLine;
                msg += GetTranslation("Reject") + ": " + ((rej.Count > 0) ? rej.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y) : "");
                msg += Environment.NewLine + Environment.NewLine;
            }
            return msg;
        }

        public void EndGame()
        {
            var msg = "";

            OldMissions.Add(CurrentMission);
            if (OldMissions.Count(x => x.MissionSuccess == true) >= 3)
            {
                // Good guys succeed, bad guys prepare to kill merlin
                Send(GetTranslation("MissionThreeSuccess", Players.Where(
                    x => x.Team == AvalonTeam.Bad).Select(x => x.GetName()).Aggregate(
                    (x, y) => x + ", " + y), Constants.KillMerlinTime));
                var p = Players.FirstOrDefault(x => new AvalonRole[] { AvalonRole.Assassin, AvalonRole.Morgause }.Contains(x.Role));
                CleanChoice(p);
                SendMenu(p, GetTranslation("BadChooseMerlin"), GenerateKillMerlinMenu(p), QuestionType.KillMerlin);
                for (int i = 0; i < Constants.KillMerlinTime; i++)
                {
                    Thread.Sleep(1000);
                    if (p.CurrentQuestion != null)
                        break;
                }
                if (p.Choice1 != 0)
                {
                    // chose merlin
                    var merlinGuess = Players.FirstOrDefault(x => x.TelegramUserId == p.Choice1);
                    Send(GetTranslation("KillMerlinDone", p.GetName(), merlinGuess.GetName()) + (merlinGuess.Role == AvalonRole.Merlin ? GetTranslation("KiledMerlinYes") : GetTranslation("KilledMerlinNo")));
                    WinningTeam = merlinGuess.Role == AvalonRole.Merlin ? AvalonTeam.Bad : AvalonTeam.Good;
                    msg = merlinGuess.Role == AvalonRole.Merlin ? GetTranslation("EndGameKilledMerlin") : GetTranslation("EndGameGoodWon");
                }
                else
                {
                    Send(GetTranslation("KillMerlinTimesUp", p.GetName()));
                    WinningTeam = AvalonTeam.Good;
                    msg = GetTranslation("EndGameGoodWon");
                }

            }
            if (OldMissions.Count(x => x.MissionSuccess == false) >= 3)
            {
                // bad guys won
                WinningTeam = AvalonTeam.Bad;
                msg = GetTranslation("EndGameThreeFails");
            }


            msg += Environment.NewLine + GenerateMissionPlayerList(true);

            Send(msg);

            Phase = GamePhase.Ending;
        }
        #endregion

        #region Helpers
        public void HandleMessage(Message msg)
        {
            if (msg.Text.ToLower().StartsWith("/join"))
            {
                if (Phase == GamePhase.Joining)
                    Reply(Pinned.MessageId, GetTranslation("NewJoinMethod"));
            }
            else if (msg.Text.ToLower().StartsWith("/flee"))
            {
                if (Phase == GamePhase.Joining)
                    RemovePlayer(msg.From);

                else if (Phase == GamePhase.InGame)
                    Send(GetTranslation("CantFleeRunningGame"));
            }
            else if (msg.Text.ToLower().StartsWith("/startgame"))
            {
                if (Phase == GamePhase.Joining)
                    Reply(Pinned.MessageId, GetTranslation("NewJoinMethod"));
            }
            else if (msg.Text.ToLower().StartsWith("/forcestart"))
            {
                if (this.Players.Count() >= 3) Phase = GamePhase.InGame;
                else
                {
                    Send(GetTranslation("GameEnded"));
                    Phase = GamePhase.Ending;
                    Bot.Gm.RemoveGame(this);
                }
            }
            else if (msg.Text.ToLower().StartsWith("/killgame"))
            {
                Send(GetTranslation("KillGame"));
                if (BotIsGroupAdmin)
                    BotMethods.PinMessage(ChatId, OldPinnedMsgId);
                Phase = GamePhase.Ending;
                Bot.Gm.RemoveGame(this);
            }
            else if (msg.Text.ToLower().StartsWith("/questhistory"))
            {
                if (Phase != GamePhase.InGame)
                {
                    msg.Reply(GetTranslation("QuestHistoryNoMission"));
                    return;
                }
                if (OldMissions.Count <= 0)
                {
                    msg.Reply(GetTranslation("QuestHistoryNoMission"));
                    return;
                }
                if (QuestHistoryMsgId == 0)
                {
                    // first
                    var m = GenerateQuestHistory();
                    var sent = msg.Reply(m);
                    QuestHistoryMsgId = sent.MessageId;
                    return;
                }
                else
                {
                    if (_questHistoryChanged)
                    {
                        var m = GenerateQuestHistory();
                        var sent = msg.Reply(m);
                        QuestHistoryMsgId = sent.MessageId;
                        _questHistoryChanged = false;
                        return;
                    }
                    else
                        Reply(QuestHistoryMsgId, GetTranslation("QuestHistoryRecent"));
                    return;
                }
            }
        }

        public void HandleQuery(CallbackQuery query, string[] args)
        {
            // args[0] = GameGuid
            // args[1] = playerId
            // args[2] = gameActionType
            // args[3] = cardId / playerId
            if (args.Length == 4)
            {
                AvalonPlayer p = Players.FirstOrDefault(x => x.TelegramUserId == Int32.Parse(args[1]));

                if (p != null && p.CurrentQuestion != null)
                {

                    switch (args[2])
                    {
                        case "king":
                            var chosen = int.Parse(args[3]);
                            var chosenp = Players.First(x => x.TelegramUserId == chosen);
                            Send(GetTranslation("MissionKingChosen", GetName(p), GetName(chosenp)));
                            if (p.Choice1 == 0)
                            {
                                p.Choice1 = chosen;
                                Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                                SendMenu(p, GetTranslation("MissionStartKingChoosePM", 2, CurrentMission.NumOfPlayers), GenerateKingChoiceMenu(p), QuestionType.King);
                                break;
                            }
                            else if (p.Choice1 != 0 && p.Choice2 == 0)
                            {
                                p.Choice2 = chosen;
                                Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                                if (CurrentMission.NumOfPlayers > 2)
                                    SendMenu(p, GetTranslation("MissionStartKingChoosePM", 3, CurrentMission.NumOfPlayers), GenerateKingChoiceMenu(p), QuestionType.King);
                                else
                                    p.CurrentQuestion = null;
                                break;
                            }
                            else if (p.Choice1 != 0 && p.Choice2 != 0 && p.Choice3 == 0)
                            {
                                p.Choice3 = chosen;
                                Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                                if (CurrentMission.NumOfPlayers > 3)
                                    SendMenu(p, GetTranslation("MissionStartKingChoosePM", 4, CurrentMission.NumOfPlayers), GenerateKingChoiceMenu(p), QuestionType.King);
                                else
                                    p.CurrentQuestion = null;
                                break;
                            }
                            else if (p.Choice1 != 0 && p.Choice2 != 0 && p.Choice3 != 0 && p.Choice4 == 0)
                            {
                                p.Choice4 = chosen;
                                Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                                if (CurrentMission.NumOfPlayers > 4)
                                    SendMenu(p, GetTranslation("MissionStartKingChoosePM", 5, CurrentMission.NumOfPlayers), GenerateKingChoiceMenu(p), QuestionType.King);
                                else
                                    p.CurrentQuestion = null;
                                break;
                            }
                            else if (p.Choice1 != 0 && p.Choice2 != 0 && p.Choice3 != 0 && p.Choice4 != 0 && p.Choice5 == 0)
                            {
                                p.Choice5 = chosen;
                                Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                                p.CurrentQuestion = null;
                                break;
                            }
                            break;
                        case "domission":
                            var suc = args[3];
                            if (suc == "success")
                                p.Success = true;
                            else if (suc == "fail")
                                p.Fail = true;
                            else if (suc == "failtwice")
                                p.FailTwice = true;
                            Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                            p.CurrentQuestion = null;
                            break;
                        case "kill":
                            chosen = int.Parse(args[3]);
                            p.Choice1 = chosen;
                            Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                            p.CurrentQuestion = null;
                            break;
                        case "lake":
                            chosen = int.Parse(args[3]);
                            p.LakeChoice = chosen;
                            Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                            p.CurrentQuestion = null;
                            break;
                        case "lakedeclare":
                            if (args[3] == "yes")
                                p.LakeDeclareChoice = true;
                            else
                                p.LakeDeclareChoice = false;
                            Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                            p.CurrentQuestion = null;
                            break;
                        default:
                            break;
                    }
                    // Bot.Edit(p.TelegramUserId, p.CurrentQuestion.MessageId, GetTranslation("ReceivedButton"));
                }
                else
                {
                    Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("NotInGame"), true);
                }
            }
            else
            {
                switch (args[1])
                {
                    case "join":
                        var newp = Players.FirstOrDefault(x => x.TelegramUserId == query.From.Id);
                        if (newp != null)
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("AlreadyJoined"), true);
                        else
                        {
                            AddPlayer(query.From);
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("ReceivedButton"));
                        }
                        break;
                    case "flee":
                        var fleep = Players.FirstOrDefault(x => x.TelegramUserId == query.From.Id);
                        if (fleep != null)
                        {
                            if (Phase == GamePhase.Joining)
                            {
                                RemovePlayer(query.From);
                                Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("ReceivedButton"));
                            }
                            else if (Phase == GamePhase.InGame)
                                Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("CantFleeRunningGame"));
                        }
                        else
                        {
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("NotInGame"), true);
                        }
                        break;
                    case "ready":
                        var readyp = Players.FirstOrDefault(x => x.TelegramUserId == query.From.Id);
                        if (readyp != null)
                        {
                            if (Phase == GamePhase.Joining)
                            {
                                readyp.Ready = true;
                                Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("ReceivedButton"));
                                Pinned.Edit(GeneratePinned(), GeneratePinnedMenu());
                            }
                        }
                        else
                        {
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("NotInGame"), true);
                        }
                        break;
                    case "wait":
                        var waitp = Players.FirstOrDefault(x => x.TelegramUserId == query.From.Id);
                        if (waitp != null)
                        {
                            if (Phase == GamePhase.Joining)
                            {
                                waitp.Ready = false;
                                Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("ReceivedButton"));
                                Pinned.Edit(GeneratePinned(), GeneratePinnedMenu());
                            }
                        }
                        else
                        {
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("NotInGame"), true);
                        }
                        break;
                    case "approval":
                        var p = Players.First(x => x.TelegramUserId == query.From.Id);
                        if (p != null)
                        {
                            if (args[2] == "approve")
                            {
                                p.Approve = true;
                            }
                            else if (args[2] == "reject")
                            {
                                p.Approve = false;
                            }
                            if (!MissionApprovalChosen.Contains(query.From.Id))
                            {
                                MissionApprovalChosen.Add(query.From.Id);
                                approvalMsgEdit = true;
                            }
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("ReceivedButton"));
                            if (approvalMsgEdit)
                            {
                                var msg = GetTranslation("ApproveReject") + Environment.NewLine;
                                msg += $"{GetTranslation("AlreadyApproval")}: {MissionApprovalChosen.Select(x => Players.First(z => z.TelegramUserId == x).GetName()).Aggregate((x, y) => x + ", " + y)}";

                                query.Message.Edit(msg, GenerateApprovalMenu());
                                approvalMsgEdit = false;
                            }
                        }
                        else
                        {
                            Bot.Api.AnswerCallbackQueryAsync(query.Id, GetTranslation("NotInGame"), true);
                        }
                        break;
                }
            }
        }

        public Message Send(string msg)
        {
            return Bot.Send(ChatId, msg);
        }

        public Message Announce(string msg)
        {
            Pinned.Edit(msg);
            return Bot.Send(ChatId, msg);
        }

        public Message SendPM(AvalonPlayer p, string msg)
        {
            return Bot.Send(p.TelegramUserId, msg);
        }

        public Message SendMenu(AvalonPlayer p, string msg, InlineKeyboardMarkup markup, QuestionType qType)
        {
            var sent = Bot.Send(p.TelegramUserId, msg, markup);
            p.CurrentQuestion = new QuestionAsked
            {
                Type = qType,
                MessageId = sent.MessageId
            };
            return sent;
        }

        public Message SendMenu(string msg, InlineKeyboardMarkup markup, QuestionType qType)
        {
            var sent = Bot.Send(ChatId, msg, markup);
            this.CurrentQuestion = new QuestionAsked
            {
                Type = qType,
                MessageId = sent.MessageId
            };
            return sent;
        }

        public Message Reply(int oldMessageId, string msg)
        {
            return BotMethods.Reply(ChatId, oldMessageId, msg);
        }

        public Message SendTimesUp(AvalonPlayer p, int currentQuestionMsgId)
        {
            return Bot.Edit(p.TelegramUserId, currentQuestionMsgId, GetTranslation("TimesUpButton"));
        }


        public InlineKeyboardMarkup GenerateMenu(AvalonPlayer p, List<AvalonPlayer> players, GameAction action)
        {
            var buttons = new List<Tuple<string, string>>();
            foreach (AvalonPlayer player in players)
            {
                buttons.Add(new Tuple<string, string>(player.Name, $"{this.Id}|{p.TelegramUserId}|{(int)action}|{player.TelegramUserId}"));
            }
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < buttons.Count; i++)
            {
                row.Clear();
                row.Add(new InlineKeyboardCallbackButton(buttons[i].Item1, buttons[i].Item2));
                rows.Add(row.ToArray());
            }
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        public InlineKeyboardMarkup GenerateStartMe()
        {
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();
            row.Add(new InlineKeyboardUrlButton(GetTranslation("StartMe"), $"https://telegram.me/{Bot.Me.Username}"));
            rows.Add(row.ToArray());
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        public string GeneratePlayerList(bool endGame = false)
        {
            try
            {
                var msg = "";
                if (!endGame) msg = GetTranslation("CurrentSequence") + Environment.NewLine;
                List<string> playerList = new List<string>();
                var pq = PlayerQueue.ToList();
                if (!endGame)
                {
                    for (int i = 1; i < pq.Count; i++)
                    {
                        playerList.Add(GetName(pq[i]));
                    }
                    // add king first
                
                    msg += $"{GetName(pq[0])} {GetTranslation("KingSymbol")}" + GetTranslation("SequenceJoinSymbol") + Environment.NewLine;
                    msg += $"{playerList.Aggregate((x, y) => x + GetTranslation("SequenceJoinSymbol") + Environment.NewLine + y)}";
                }
                else
                {
                    foreach (AvalonPlayer p in pq)
                    {
                        msg += $"{(p.Team == AvalonTeam.Good ? GetTranslation("Good") : GetTranslation("Bad"))} {p.GetName()} - {GetTranslation(p.Role.ToString())} {(p.Team == WinningTeam ? GetTranslation("Won") : GetTranslation("Lost"))}" + Environment.NewLine;
                    }
                }
                return msg;
            }
            catch (Exception ex)
            {
                Log(ex);
                return "";
            }
        }

        public string GeneratePinned()
        {
            try
            {
                var msg = GetTranslation("NewGame") + Environment.NewLine;
                msg += GetTranslation("PlayerList") + Environment.NewLine;
                if (Players.Count > 0)
                    msg += Players.Select(x => x.GetName() + (x.Ready ? ($" {GetTranslation("ReadyButton")}") : "")).Aggregate((x, y) => x + Environment.NewLine + y);
                else
                    msg += GetTranslation("NoOnePlay");
                return msg;
            }
            catch (Exception e)
            {
                e.LogError();
                return "";
            }
        }

        public InlineKeyboardMarkup GeneratePinnedMenu()
        {
            var buttons = new List<InlineKeyboardButton>();
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("JoinButton"), $"{this.Id}|join"));
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("FleeButton"), $"{this.Id}|flee"));
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("ReadyButton"), $"{this.Id}|ready"));
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("WaitButton"), $"{this.Id}|wait"));
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    twoMenu.Add(new[] { buttons[i] });
                }
                else
                    twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }
            return new InlineKeyboardMarkup(twoMenu.ToArray());
        }

        public string GetName(AvalonPlayer p)
        {
            return Helper.GetName(p);
        }

        public string GetName(User p)
        {
            return Helper.GetName(p);
        }


        public void Dispose()
        {
            Players?.Clear();
            Players = null;
            PlayerQueue?.Clear();
            PlayerQueue = null;
            Winner = null;
            // MessageQueueing = false;
        }

        public void Log(Exception ex)
        {
            Helper.LogError(ex);
            Send("Sorry there is some problem with me, I gonna go die now.");
            this.Phase = GamePhase.Ending;
            Bot.Gm.RemoveGame(this);
        }
        #endregion

        #region Language related
        public void LoadLanguage(string language)
        {
            try
            {
                var files = Directory.GetFiles(Constants.GetLangDirectory());
                var file = files.First(x => Path.GetFileNameWithoutExtension(x) == language);
                {
                    var doc = XDocument.Load(file);
                    Locale = new Locale
                    {
                        Language = Path.GetFileNameWithoutExtension(file),
                        File = doc
                    };
                }
                Language = Locale.Language;
            }
            catch
            {
                if (language != "English")
                    LoadLanguage("English");
            }
        }

        private string GetTranslation(string key, params object[] args)
        {
            try
            {
                var strings = Locale.File.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key) ??
                              Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                if (strings != null)
                {
                    var values = strings.Descendants("value");
                    var choice = Helper.RandomNum(values.Count());
                    var selected = values.ElementAt(choice).Value;

                    return String.Format(selected, args).Replace("\\n", Environment.NewLine);
                }
                else
                {
                    throw new Exception($"Error getting string {key} with parameters {(args != null && args.Length > 0 ? args.Aggregate((a, b) => a + "," + b.ToString()) : "none")}");
                }
            }
            catch (Exception e)
            {
                try
                {
                    //try the english string to be sure
                    var strings =
                        Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                    var values = strings?.Descendants("value");
                    if (values != null)
                    {
                        var choice = Helper.RandomNum(values.Count());
                        var selected = values.ElementAt(choice - 1).Value;
                        // ReSharper disable once AssignNullToNotNullAttribute
                        return String.Format(selected, args).Replace("\\n", Environment.NewLine);
                    }
                    else
                        throw new Exception("Cannot load english string for fallback");
                }
                catch
                {
                    throw new Exception(
                        $"Error getting string {key} with parameters {(args != null && args.Length > 0 ? args.Aggregate((a, b) => a + "," + b.ToString()) : "none")}",
                        e);
                }
            }
        }

        #endregion
        #region Constants

        public enum GamePhase
        {
            Joining, InGame, Ending
        }

        public enum GameAction
        {
            King, ApproveMission, DoMission, Lake, Ending
        }

        #endregion
    }
}