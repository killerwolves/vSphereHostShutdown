using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net;

namespace vSphereHostShutdown
{
    class CookieAwareWebClient : WebClient
    {
        private CookieContainer cookies = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = cookies;
                this.BaseAddress = address.AbsoluteUri;
            }
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            return base.GetWebResponse(request);
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            return base.GetWebResponse(request, result);
        }
    }
}
