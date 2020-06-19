using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Parser.UI
{    public class ProxyItem
    {
        public string ip { get; set; }
        public int port { get; set; }
        public int lastseen { get; set; }
        public int delay { get; set; }
        public string country_code { get; set; }
        public string country_name { get; set; }
        public string city { get; set; }
        public int anon { get; set; }
    }

    public sealed class ProxyItemMap : CsvClassMap<ProxyItem>
    {
        public ProxyItemMap()
        {
            Map(m => m.ip);
            Map(m => m.port);
            Map(m => m.lastseen);
            Map(m => m.delay);
            Map(m => m.country_code);
            Map(m => m.country_name);
            Map(m => m.city);
            Map(m => m.anon);
        }
        public static void initConfiguration(CsvConfiguration csvConfiguration)
        {
            csvConfiguration.RegisterClassMap<ProxyItemMap>();
            csvConfiguration.TrimFields = true;
        }
    }

    public class Proxies
    {
        private readonly ProxyItem[] _proxyDef = null;
        public Proxies(string file)
        {
            var fileName = Path.Combine(Environment.CurrentDirectory, file);
            if (File.Exists(fileName))
            {
                using (TextReader reader = File.OpenText(file))
                {
                    using (var csvReader = new CsvReader(reader))
                    {
                        ProxyItemMap.initConfiguration(csvReader.Configuration);
                        csvReader.Read();
                        _proxyDef = csvReader.GetRecords<ProxyItem>().ToArray();
                    }
                }
            }
            else
            {
                _proxyDef = new ProxyItem[] {null };
            }
        }

        public ProxyItem GetItem()
        {
            var random = new Random();
            var i = random.Next(0, _proxyDef.Count() - 1);
            return _proxyDef[i];

        }

    }
}
