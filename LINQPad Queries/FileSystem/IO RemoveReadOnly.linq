<Query Kind="Statements">
  
</Query>

var location= System.IO.Directory.GetFiles(@"\\crprdnii1i4\sites\gtpm-init1\wwwroot\site");

location.Dump();
var webconfig=location.First(e=>e.EndsWith("Web.config"));
var attributes=System.IO.File.GetAttributes(webconfig);
attributes.Dump();
if((attributes & FileAttributes.ReadOnly)== FileAttributes.ReadOnly)
System.IO.File.SetAttributes(webconfig, attributes ^ FileAttributes.ReadOnly);