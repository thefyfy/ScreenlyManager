using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenlyManager
{
    public class Device
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "ip_address")]
        public string IpAddress { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "port")]
        public string Port { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        public string HttpLink
        {
            get
            {
                return "http://" + IpAddress + ":" + Port;
            }
        }
    }
}
