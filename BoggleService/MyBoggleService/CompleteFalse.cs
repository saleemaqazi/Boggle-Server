using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BoggleService.Models
{
    public class CompleteFalse
    {
        public string GameState { get; set; }
        public string Board { get; set; }
        public int TimeLimit { get; set; }

        public PlayerReturn3 Player1;
        public PlayerReturn3 Player2;

    }

    public class PlayerReturn3
    {

        public string Nickname { get; set; }

        public int Score { get; set; }

        public List<Word1> WordsPlayed { get; set; }
        
    }

    public class Word1
    {
        public string Word { get; set; }
        public int Score { get; set; }
    }
}