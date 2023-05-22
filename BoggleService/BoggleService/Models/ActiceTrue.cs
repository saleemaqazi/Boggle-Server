using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BoggleService.Models
{
    public class ActiceTrue
    {
        public string GameState { get; set; }
        public int TimeLeft { get; set; }

        public PlayerReturn Player1;

        public PlayerReturn Player2;

    }

    public class PlayerReturn
    {
        public int Score { get; set; }
    }

}