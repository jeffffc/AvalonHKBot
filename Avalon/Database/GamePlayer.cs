//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class GamePlayer
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public bool Won { get; set; }
        public bool Accomplice { get; set; }
        public string FinalTeam { get; set; }
        public string SpecialRole { get; set; }
    
        public virtual Game Game { get; set; }
        public virtual Player Player { get; set; }
    }
}