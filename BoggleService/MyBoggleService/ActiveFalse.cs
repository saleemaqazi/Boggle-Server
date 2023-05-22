using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BoggleService.Models
{
    public class ActiveFalse
    {
        public string GameState { get; set; }
        public int TimeLeft { get; set; }

        public PlayerReturn2 Player1;

        public PlayerReturn2 Player2;

        public int TimeLimit { get; set; }

        public string Board { get; set; }
    }

    public class PlayerReturn2
    {
        public int Score { get; set; }

        public string Nickname { get; set; }
    }
}