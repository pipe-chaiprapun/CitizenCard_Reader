using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ThaiNationalIDCard
{
    class PostToPHP
    {
        //this is proxy setting 
        private bool proxy_setting = false;//not connected with proxy server

        //this is your proxy username,password,ip
        private string proxy_ip = "http://192.168.1.1";//your proxy server
        private string port = "80"; //proxy password
        private string username = "username"; //proxy username
        private string password = "password";  //proxy password 

        //this is for where you want to pass your date URL
        private string post_url = "http://localhost/SimpleRESTful/cash.php";
        public string DataRequestToServer(string postData, string url)
        {
            try
            {
                // Create a new request to the mentioned URL.    
                HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(url);
                if (proxy_setting == true)
                {
                    connectProxy(httpWReq);
                }
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] data = encoding.GetBytes(postData);
                httpWReq.Method = "POST";
                httpWReq.ContentType = "application/x-www-form-urlencoded";
                httpWReq.ContentLength = data.Length;
                using (Stream stream = httpWReq.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();
                string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return responseString;

            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public void connectProxy(HttpWebRequest httpWReq)
        {
            // Obtain the 'Proxy' of the  Default browser.  
            IWebProxy proxy = httpWReq.Proxy;
            WebProxy myProxy = new WebProxy();
            Uri newUri = new Uri(proxy_ip);
            // Associate the newUri object to 'myProxy' object so that new myProxy settings can be set.
            myProxy.Address = newUri;
            // Create a NetworkCredential object and associate it with the  
            // Proxy property of request object.
            myProxy.Credentials = new NetworkCredential(username, password);
            httpWReq.Proxy = myProxy;
        }
    }
}
