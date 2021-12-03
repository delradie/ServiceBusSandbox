using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestbedContracts
{
    public class RequestMessage
    {
        public String MessageText { get; set;}

        public DateTimeOffset Timestamp { get; set; }
    }
}
