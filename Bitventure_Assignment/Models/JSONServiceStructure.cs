using System;
using System.Collections.Generic;
using System.Text;

namespace Bitventure_Assignment.Models
{
    class JSONServiceStructure
    {

        public class Root
        {
            public Service[] services { get; set; }
        }

        public class Service
        {
            public string baseURL { get; set; }
            public string datatype { get; set; }
            public bool enabled { get; set; }
            public Endpoint[] endpoints { get; set; }
            public Identifier[] identifiers { get; set; }
        }

        public class Endpoint
        {
            public bool enabled { get; set; }
            public string resource { get; set; }
            public Response[] response { get; set; }
            public string requestBody { get; set; }
        }

        public class Response
        {
            public string element { get; set; }
            public string identifier { get; set; }
            public string regex { get; set; }
        }

        public class Identifier
        {
            public string key { get; set; }
            public string value { get; set; }
        }

    }
}
