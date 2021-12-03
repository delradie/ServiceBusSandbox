using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestbedContracts
{
    public class ResponseMessage
    {
        public String ResponseText { get; set; }

        public String OriginalMessageText { get; set; }
        public DateTimeOffset RequestTimestamp { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
