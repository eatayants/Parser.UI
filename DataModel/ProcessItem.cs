using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.UI.DataModel
{
    public class ProcessItem
    {
        public string Url { get; set; }
        public DateTime Tick { get; set; }
        public string Message { get; set; }
        public string Title { get; set; }
    }
}
