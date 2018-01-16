using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvalonHKBot.Models
{
    public class AvalonMission
    {
        public int MissionNum { get; set; }
        public int NumOfPlayers { get; set; }
        public bool? MissionSuccess { get; set; }
        public bool MissionDone { get; set; } = false;
        public AvalonPlayer MissionDoneKing { get; set; }
        public List<AvalonPlayer> MissionDoneBy { get; set; } = new List<AvalonPlayer>();
        public List<AvalonMissionAttempt> Attempts { get; set; } = new List<AvalonMissionAttempt>();

    }

    public class AvalonMissionAttempt
    {
        public bool Approved { get; set; } = false;
        public List<AvalonPlayer> ApprovedBy { get; set; }
        public List<AvalonPlayer> RejectedBy { get; set; }
    }
}
