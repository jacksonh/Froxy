
using System;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Manos;
using Manos.Http;

namespace Froxy {

	public class Froxy : ManosApp {

		public class CurlResult {
			public string Header;
			public string Body;
			public string Request;
		}

		public Froxy ()
		{
			Route ("/Content/", new StaticContentModule ());
		}

		[Get ("/")]
		public void Index (IManosContext ctx)
		{
			ctx.Response.SendFile ("Templates/index.html");
			ctx.Response.End ();
		}

		[Post ("/")]
		public void Curl (IManosContext ctx, string url, string method)
		{
			{
				// TODO: Manos will handle this DNS stuff internally soon.

				Uri u = null;
				if (!Uri.TryCreate (url, UriKind.Absolute, out u)) {
					CurlError (ctx, "Url is invalid: {0}", url);
					return;
				}

				IPAddress [] addrs = Dns.GetHostAddresses (u.Host);
				if (addrs.Length == 0) {
					CurlError (ctx, "Could not resolve host: {0}", u.Host);
					return;
				}

				UriBuilder builder = new UriBuilder (u);
				builder.Host = addrs [0].ToString ();
				
				url = builder.ToString ();
			}

			HttpRequest r = new HttpRequest (url) {
				Method = GetHttpMethod (method),
			};

			r.OnResponse += (response) => {

				var res = new Dictionary<object,object> ();

				res ["header"] = PrettyPrintHeaders (response.Headers);
				res ["request"]  = "I am the request";
				res ["body"] = "<div><pre>" + HttpUtility.HtmlEncode (response.PostBody) + "</pre></div>";

				ctx.Response.End (JSON.JsonEncode (res));
			};

			try {
				r.Execute ();
			} catch (Exception e) {
				CurlError (ctx, e.Message);
			}
			
		}

		[Ignore]
		private void CurlError (IManosContext ctx, string message, params string [] p)
		{
			var res = new Dictionary<object,object> ();

			res ["error"] = String.Format (message, p);

			ctx.Response.End (JSON.JsonEncode (res));
		}

		private HttpMethod GetHttpMethod (string method)
		{
			switch (method) {
			case "GET":
				return HttpMethod.HTTP_GET;
			case "PUT":
				return HttpMethod.HTTP_PUT;
			case "POST":
				return HttpMethod.HTTP_POST;
			case "HEAD":
				return HttpMethod.HTTP_HEAD;
			case "DELETE":
				return HttpMethod.HTTP_DELETE;
			default:
				throw new ArgumentException ("Unknown http method: " + method);
			}
		}

		private string PrettyPrintHeaders (HttpHeaders headers)
		{
			StringBuilder res = new StringBuilder ();

			res.Append ("<div class='highlight'><pre>");
			
			foreach (string key in headers.Keys) {
				string value;
				if (headers.TryGetValue (key, out value))
					res.AppendFormat ("<span class='nt'>{0}</span>:<span class='s'>{1}\n</span>", key, value);
				else
					res.AppendFormat ("<span class='nf'>{0}\n</span>", key);
			}

			res.Append ("</pre></div>");

			return res.ToString ();
		}
	}
}
