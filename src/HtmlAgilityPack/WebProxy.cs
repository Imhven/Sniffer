using System;
using System.Net;

namespace HtmlAgilityPack
{
    public class WebProxy : IWebProxy
    {
        private Uri _uri = null;
        public WebProxy(string Host, int Port)
        {
            if (Host.StartsWith("http"))
            {
                _uri = new Uri(Host + ":" + Port);
            }
            else
            {
                _uri = new Uri("http://" + Host + ":" + Port);
            }
        }
        public Uri GetProxy(Uri destination)
        { 
            return _uri;
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }

        public ICredentials Credentials { get; set; }
    }
}