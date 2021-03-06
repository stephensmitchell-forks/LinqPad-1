<Query Kind="FSharpProgram">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>Suave</NuGetReference>
</Query>

//let dc = new TypedDataContext()
//dc.Users.Dump()
// imaginarydevelopment.blogspot.com
// @maslowjax

let replace (target:string) (replacement:string) (text:string) = text.Replace(target,replacement)
open Suave
open Suave.Files
open Suave.Filters
open Suave.Successful
open Suave.Operators
open Suave.Authentication

//needs: serve files, serve pages

module ServerSamples = 
    
    
        
    let logger = // https://github.com/SuaveIO/suave/blob/releases/v1.x/src/Suave/Logging/LogLine.fs ?
//        let l = Suave.Logging.Loggers.OutputWindowLogger(Suave.Logging.LogLevel.Debug)
//        Suave.Logging.Loggers.ConsoleWindowLogger Suave.Logging.LogLevel.Debug
//        l
        { new Suave.Logging.Logger with
            member __.Log _lvl f = 
                let toLog = f()
                match toLog.``exception``, toLog.message.EndsWith("404 0") with
                | None, true -> printfn "%A" toLog
                | None, false -> ()
                | Some x, _ -> printfn "Exception: %A" x
        }
    
    let xpressAuth () = 
        /// https://github.com/h5bp/html5-boilerplate/blob/master/src/index.html
        let html = """<!doctype html>
<html class="no-js" lang="">
    <head>
        <meta charset="utf-8">
        <meta http-equiv="x-ua-compatible" content="ie=edge">
        <title></title>
        <meta name="description" content="">
        <meta name="viewport" content="width=device-width, initial-scale=1">

        <link rel="apple-touch-icon" href="apple-touch-icon.png">
        <!-- Place favicon.ico in the root directory -->

        <link rel="stylesheet" href="css/normalize.css">
        <link rel="stylesheet" href="css/main.css">
        <link rel="stylesheet" href="Content/Site.css">
        <script src="js/vendor/modernizr-2.8.3.min.js"></script>
    </head>
    <body>
        <!--[if lt IE 8]>
            <p class="browserupgrade">You are using an <strong>outdated</strong> browser. Please <a href="http://browsehappy.com/">upgrade your browser</a> to improve your experience.</p>
        <![endif]-->

        <!-- Add your site or application content here -->
        <p>Hello world! This is HTML5 Boilerplate.</p>
        <h2> Requires auth</h2>
        <ul>
            <li><a href="/whereami">who am i</a></li>
            <li><a href="/dirHome">dirHome</a></li>
        </ul>
        <script src="https://code.jquery.com/jquery-{{JQUERY_VERSION}}.min.js"></script>
        <script>window.jQuery || document.write('<script src="js/vendor/jquery-{{JQUERY_VERSION}}.min.js"><\/script>')</script>
        <script src="js/plugins.js"></script>
        <script src="js/main.js"></script>

        <!-- Google Analytics: change UA-XXXXX-Y to be your site's ID. -->
        <script>
            window.ga=function(){ga.q.push(arguments)};ga.q=[];ga.l=+new Date;
            ga('create','UA-XXXXX-Y','auto');ga('send','pageview')
        </script>
        <script src="https://www.google-analytics.com/analytics.js" async defer></script>
    </body>
</html>"""

        let authenticate (userName:string,password:string) : bool = ("foo","bar") = (userName,password)
        let requiresAuthentication _ =
            choose
                [ GET >=> path "/" >=> OK (html 
                                            |> replace "js/vendor/modernizr-2.8.3.min.js" "/Scripts/modernizr-2.8.3.js" 
                                            |> replace "css/normalize.css" "//cdnjs.cloudflare.com/ajax/libs/normalize/4.1.1/normalize.min.css"
                                            |> replace "src=\"js/" "src=\"Scripts/"
                                            |> replace "href=\"css/" "href=\"Content/" 
                                            |> replace "{{JQUERY_VERSION}}" "latest"
                                                )
                  GET >=> pathStarts  "/Scripts" >=> choose [
                                                                browseHome 
                                                                Response.response HttpCode.HTTP_404 Array.empty]
                  
                  GET >=> pathStarts  "/Content" >=> choose [
                                                                browseHome 
                                                                Response.response HttpCode.HTTP_404 Array.empty]
                  GET >=> path "/favicon.ico" >=> Suave.Response.response HttpCode.HTTP_404 null
                  
                  // access to handlers after this one will require authentication
                  Authentication.authenticateBasic authenticate <|
                    choose [
                            GET >=> path "/whereami" >=> OK (sprintf "Hello authenticated person")
                            GET >=> pathStarts "/dirHome" >=> dirHome
                            GET >=> browseHome //serves file if exists                       
                    ]
                ] >=> log logger logFormat
        let rootPath = Path.Combine(Environment.ExpandEnvironmentVariables "%devroot%", @"PracticeManagement\dev\PracticeManagement\Pm.Web\")
        printfn "rootPath is %s" rootPath
        printfn "directories under root: %A" (Directory.GetDirectories rootPath)
        requiresAuthentication()
        |> startWebServer { defaultConfig with homeFolder = Some rootPath}
            
LINQPad.Hyperlinq("http://localhost:8083").Dump()
LINQPad.Hyperlinq("http://localhost:8083/public").Dump()
ServerSamples.xpressAuth()