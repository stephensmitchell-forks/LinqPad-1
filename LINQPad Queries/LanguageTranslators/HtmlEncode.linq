<Query Kind="FSharpExpression">
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <Namespace>System.Web</Namespace>
</Query>

//var input=LINQPad.Util.ReadLine<string>("What shall we encode?");
let input=System.Windows.Forms.Clipboard.GetText()
printfn "%s%s%s" "<pre class='brush: csharp'>"(System.Net.WebUtility.HtmlEncode(input)) "</pre>"
printfn "%s%s%s" "<code>" (System.Net.WebUtility.HtmlEncode(input)) "</code>"

input.Dump("Raw");
System.Net.WebUtility.HtmlEncode(input).Dump("System.Net.WebUtility.HtmlEncode")
System.Net.WebUtility.UrlEncode(input).Dump("UrlEncode")
System.Uri.EscapeDataString(input).Dump("EscapeDataString")
System.Uri.EscapeUriString(input).Dump("EscapeUriString")
System.Web.HttpUtility.HtmlEncode(input).Dump("System.Web.HttpUtility.HtmlEncode")
System.Web.HttpUtility.UrlEncode(input).Dump("System.Web.HttpUtility.UrlEncode")
System.Web.HttpUtility.HtmlAttributeEncode(input).Dump("HtmlAttribute")