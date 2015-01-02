using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using System.IO;

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

			var request = (HttpWebRequest)HttpWebRequest.Create ("https://www.noip.com/login");

			request.Method = "GET";
			request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

			var response = (HttpWebResponse)request.GetResponse ();

			if (response.StatusCode != HttpStatusCode.OK) {
				Console.WriteLine ("ERROR: " + response.StatusCode);
			}

			var sessionIdLine = response.Headers.GetValues ("Set-Cookie").Single (l => l.IndexOf ("noip_session") == 0);

			var start = sessionIdLine.IndexOf ("=");
			var sessionId = sessionIdLine.Substring(start+1, sessionIdLine.IndexOf(";")-start);

			//foreach (var headerKey in response.Headers.AllKeys)
			//	foreach (var val in response.Headers.GetValues(headerKey))
			//		Console.WriteLine (headerKey + ":" + val);

			string token;
			using (var streamReader = new StreamReader (response.GetResponseStream ())) {
				var body = streamReader.ReadToEnd ();

				body = body.Substring (body.IndexOf ("<input type=\"hidden\" name=\"_token\" value=\"") + 42);
				token = body.Substring (0, body.IndexOf ("\">"));
			}


			Console.WriteLine ("SESSION_ID: " + sessionId);
			Console.WriteLine ("TOKEN: " + token);

			request = (HttpWebRequest) HttpWebRequest.Create ("https://www.noip.com/login");

			NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
			outgoingQueryString.Add("username",username);
			outgoingQueryString.Add("password", password);
			outgoingQueryString.Add("submit_login_page", "1");
			outgoingQueryString.Add("_token", token);
			outgoingQueryString.Add("Login","");

			string postdata = outgoingQueryString.ToString();

			ASCIIEncoding ascii = new ASCIIEncoding();
			byte[] postBytes = ascii.GetBytes(postdata.ToString());

			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = postBytes.Length;
			request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

			Stream postStream = request.GetRequestStream();
			postStream.Write(postBytes, 0, postBytes.Length);
			postStream.Flush();
			postStream.Close();

			request.CookieContainer = new CookieContainer ();
			request.CookieContainer.Add (new Cookie ("noip_session", sessionId, "/", "noip.com"));

			response = (HttpWebResponse)request.GetResponse ();

			if (response.StatusCode == HttpStatusCode.OK)
				Console.WriteLine ("LOGIN SUCCESSFUL");
			else
				Console.WriteLine ("ERROR: " + response.StatusCode);

			using (var streamReader = new StreamReader (response.GetResponseStream ())) {
				Console.WriteLine(streamReader.ReadToEnd());
			}

			Console.ReadLine ();
		}
	}
}

//NOIP_BID=54a42efb169eb4.76551720; REF_CODE=http%3A%2F%2Fwww.noip.com%2Fsign-in; NOIP_SID=20.20.11254a6ba881efe97.59003818; cookie_email=mylesmcdonnell; _gat=1; _gat_UA-31174-1=1; noip_session=d0b8cb8cb57539289f2dc5f45af6e07de931b132; _ga=GA1.2.910282303.1420046076
