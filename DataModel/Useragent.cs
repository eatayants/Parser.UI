using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Parser.UI
{
    public class UserAgent
    {
        private readonly string[] userAgentDef = null;
        public UserAgent(string file)
        {
            var fileName = Path.Combine(Environment.CurrentDirectory, file);
            if (File.Exists(fileName))
            {
                var document = XDocument.Load(fileName);
                userAgentDef = (from useragent in document.Descendants("useragent")
                select useragent.Attribute("useragent").Value).ToArray();
            }
            else
            {
                userAgentDef = new string[] { Extentions.UserAgent };
            }
        }

        public string GetItem()
        {
            var random = new Random();
            var i = random.Next(0, userAgentDef.Count()-1);
            return userAgentDef[i];

        }
            
    }
}
