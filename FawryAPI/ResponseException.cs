using System;
using System.Net.Http;

namespace SolonXpl.FawryAPI
{
    [Serializable]
    public class ResponseException : Exception
    {
        private HttpResponseMessage Response { get; set; }
        public ResponseException(HttpResponseMessage httpResponseMessage)
        {
            Response = httpResponseMessage;
        }
        public override string ToString()
        {
            var content = Response.Content.ReadAsStringAsync().Result;
            return content;
        }
    }
}