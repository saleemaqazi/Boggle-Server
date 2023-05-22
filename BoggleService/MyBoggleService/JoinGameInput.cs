using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BoggleService.Models
{
    public class JoinGameInput
    {
        public string UserToken { get; set; }
        public int TimeLimit { get; set; }
    }
}