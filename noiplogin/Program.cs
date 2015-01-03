using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.Threading;

namespace noiplogin
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			#if DEBUG
				WebRequest.DefaultWebProxy = new WebProxy("127.0.0.1", 8888);
			#endif

			if (args.Length < 2) {
				Console.WriteLine ("USAGE: noiplogin {username} {password}");
				return;
			}

			var username = args [0];
			var password = args [1];

			var request = (HttpWebRequest)WebRequest.Create ("https://www.noip.com/login");

			request.Method = "GET";
			request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

			var response = (HttpWebResponse)request.GetResponse ();

			if (response.StatusCode != HttpStatusCode.OK) {
				Console.WriteLine ("ERROR: " + response.StatusCode);
			}

			var sessionIdLine = response.Headers.GetValues ("Set-Cookie").Single (l => l.IndexOf ("noip_session") == 0);

			var start = sessionIdLine.IndexOf ("=");
			var sessionId = sessionIdLine.Substring(start+1, sessionIdLine.IndexOf(";")-(start+1));

            var noipBidLine = response.Headers.GetValues("Set-Cookie").Single(l => l.IndexOf("NOIP_BID") == 0);

            start = noipBidLine.IndexOf("=");
            var noipbid = noipBidLine.Substring(start + 1, noipBidLine.IndexOf(";") - (start + 1));

            string token;
			using (var streamReader = new StreamReader (response.GetResponseStream ())) {
				var body = streamReader.ReadToEnd ();

				body = body.Substring (body.IndexOf ("<input type=\"hidden\" name=\"_token\" value=\"") + 42);
				token = body.Substring (0, body.IndexOf ("\">"));
			}


			Console.WriteLine ("SESSION_ID: " + sessionId);
			Console.WriteLine ("TOKEN: " + token);
            Console.WriteLine("NOIP_BID: " + noipbid);

			request = (HttpWebRequest) WebRequest.Create ("https://www.noip.com/login");

			var outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
			outgoingQueryString.Add("username",username);
			outgoingQueryString.Add("password", password);
			outgoingQueryString.Add("submit_login_page", "1");
			outgoingQueryString.Add("_token", token);
			outgoingQueryString.Add("Login","");

			var postdata = outgoingQueryString.ToString();

			var ascii = new ASCIIEncoding();
			_postBytes = ascii.GetBytes(postdata);

			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

            var target = new Uri("https://www.noip.com/");

            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(new Cookie("noip_session", sessionId, "/") { Domain = target.Host });
            request.CookieContainer.Add(new Cookie("NOIP_BID", noipbid, "/") { Domain = target.Host });

            request.BeginGetRequestStream(GetRequestStreamCallback, request);

            AllDone.WaitOne();

            //TODO: confirm successful login and set exit code accordingly
            //      //   var streamResponse = _response.GetResponseStream();

            //         if (_response.StatusCode == HttpStatusCode.OK && response.ResponseUri.AbsoluteUri.IndexOf("members")>-1)
            //	Console.WriteLine ("LOGIN SUCCESSFUL");
            //else
            //	Console.WriteLine ("ERROR: " + response.StatusCode);

            //       //  streamResponse.Close();
            //         _response.Close();

#if DEBUG
            Console.ReadLine ();
#endif
		}

        private static readonly ManualResetEvent AllDone = new ManualResetEvent(false);
	    private static HttpWebResponse _response;
	    private static byte[] _postBytes;

	    private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            var request = (HttpWebRequest)asynchronousResult.AsyncState;
            var postStream = request.EndGetRequestStream(asynchronousResult);

            postStream.Write(_postBytes, 0, _postBytes.Length);
            postStream.Flush();
            postStream.Close();

            request.BeginGetResponse(GetResponseCallback, request);
        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            var request = (HttpWebRequest)asynchronousResult.AsyncState;
            _response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
            
            AllDone.Set();
        }
    }
}
