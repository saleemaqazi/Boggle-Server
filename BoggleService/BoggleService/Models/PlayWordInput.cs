using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BoggleService.Models
{
    public class PlayWordInput
    {
        public string Word { get; set; }
        public string UserToken { get; set; }
    }
}