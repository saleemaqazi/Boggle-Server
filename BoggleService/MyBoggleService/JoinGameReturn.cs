using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BoggleService.Models
{
    public class JoinGameReturn
    {
        public string GameID { get; set; }
        public bool IsPending { get; set; }
    }
}