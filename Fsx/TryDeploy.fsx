﻿open System
open System.IO
open System.Diagnostics

//idea for runProc http://stackoverflow.com/questions/15988917/how-can-i-set-the-window-text-of-an-application-using-net-process-start
//open System.Runtime.InteropServices
//
//module private Imported = 
//    [<DllImport("user32.dll")>]
//    extern bool SetWindowText(IntPtr hWnd, string text);

let combine s1 s2 = Path.Combine(s1,s2)
let combineAll ([<ParamArray>]args) = args |> Array.ofSeq |> Path.Combine
let delimit s (items:string seq) = String.Join(s,items)
let copyAllFiles sourceDir targetDir overwrite = 
    Directory.CreateDirectory targetDir |> ignore
    Directory.GetFiles(sourceDir) |> Seq.iter (fun f -> File.Copy(f, combine targetDir (Path.GetFileName f), overwrite))

let runProc filename args startDir = 
    let timer = System.Diagnostics.Stopwatch.StartNew()
    let procStartInfo = 
        ProcessStartInfo(
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            FileName = filename,
            Arguments = args
        )
    match startDir with | Some d -> procStartInfo.WorkingDirectory <- d | _ -> ()

    let outputs = System.Collections.Generic.List<string>()
    let errors = System.Collections.Generic.List<string>()
    let outputHandler f (_sender:obj) (args:DataReceivedEventArgs) = f args.Data
    let p = new Process(StartInfo = procStartInfo)
    p.OutputDataReceived.AddHandler(DataReceivedEventHandler (outputHandler outputs.Add))
    p.ErrorDataReceived.AddHandler(DataReceivedEventHandler (outputHandler errors.Add))
    let started = 
        try
            p.Start()
        with | ex ->
            ex.Data.Add("filename", filename)
            reraise()
    if not started then
        failwithf "Failed to start process %s" filename
    printfn "Started %s with pid %i" p.ProcessName p.Id
    p.BeginOutputReadLine()
    p.BeginErrorReadLine()
    p.WaitForExit()
    timer.Stop()
    printfn "Finished %s after %A milliseconds" filename timer.ElapsedMilliseconds
    let cleanOut l = l |> Seq.filter (fun o -> String.IsNullOrEmpty o |> not)
    cleanOut outputs,cleanOut errors

let msbuild targetProject buildArgs = 
    let targetFolder = Path.GetDirectoryName targetProject
    let msbuildPath = @"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
    let errorCount outputs errors = 
        let regex = System.Text.RegularExpressions.Regex(@"^\s*([1-9][0-9]*)\s+Error\(s\)$|Build FAILED.")
        [ outputs;errors] |> Seq.concat  |> Seq.map regex.Match |> Seq.tryFind(fun m -> m.Success)

    let args = targetProject::buildArgs |> delimit " "
    let output,errors = runProc msbuildPath args (Some targetFolder)
    match errorCount output errors with
    | Some errorMatch -> 
        //printfn "errorMatch (captures %i,groups %i) %A" errorMatch.Captures.Count errorMatch.Groups.Count errorMatch
        //[0..errorMatch.Captures.Count-1] |> Seq.map (fun i -> errorMatch.Captures.[i].Value) |> Seq.iter (printfn "Capture %s")
        //[0..errorMatch.Groups.Count-1] |> Seq.map (fun i -> errorMatch.Groups.[i].Value) |> Seq.iter (printfn "Group %s")
        //printfn "%A" output
        //printfn "%A" errors
        let regex = System.Text.RegularExpressions.Regex("Build error", Text.RegularExpressions.RegexOptions.IgnoreCase)

        printfn "%A" (output |> Seq.filter regex.IsMatch |> List.ofSeq)
        let errorText = 
            let text = errorMatch.Groups.[1].Value
            if String.IsNullOrWhiteSpace(text) then errorMatch.Groups.[0].Value else text
        failwithf "ErrorsFound : %s" errorText
    | None -> ()
    if output |> Seq.contains ("Build FAILED.") then failwithf "Build failed"
    output,errors

//buildDbProj sample usages:
// buildDbProj (dbFolders |> Seq.skip(1) |> Seq.head) (Some @"C:\TFS\XpressCharts\CustomBuildActivities\BCustomBuildTasks.dll");;
//dbFolders |> Seq.skip 1 |> Seq.head |> buildDbProj None
//dbFolders |> Seq.skip 1 |> Seq.head |> buildDbProj (Some @"C:\TFS\XpressCharts\CustomBuildActivities\BCustomBuildTasks.dll")
//dbFolders |> Seq.head |> buildDbProj None;;

let buildDbProj loggerPath targetFolder = 
    let dbProjFile = Directory.GetFiles(targetFolder, "*.dbproj") |> Seq.head
    let logger = 
        match loggerPath with
        | Some lp -> sprintf "/logger:XMLLogger,%s" lp
        | None -> ""
    let args = 
        logger::["/target:Build";]
        |> List.filter (String.IsNullOrWhiteSpace >> not)
    let outputs,errors = msbuild dbProjFile args
    printfn "%A" (Array.ofSeq outputs)
    //printfn "error length: %i" (Seq.length errors)
    if Seq.exists (fun _ -> true) errors then
        printfn "errors %A" (Array.ofSeq errors)
        failwithf "Build failed %A" errors
    let xml = outputs |> Seq.last
    targetFolder,xml

let findNewest path = 
    Directory.GetFiles path
    |> Seq.map File.GetLastWriteTime
    |> Seq.max

let copyDbProjOutputs dbProjFolder target = //assume all output to a /sql folder
    let sqlFolder = combine dbProjFolder "sql"
    let newest = 
        [combine sqlFolder "debug"; combine sqlFolder "release"]
        |> Seq.filter Directory.Exists
        |> Seq.filter (Directory.GetFiles >> Seq.exists(fun _ -> true))
        |> Seq.maxBy findNewest
    copyAllFiles newest target true

let runDeploy cmd = 
    let cmdText = File.ReadAllLines(cmd)
    let badLines = cmdText |> Seq.mapi (fun i line -> (i,line),line.TrimStart().StartsWith("pause")) |> Seq.filter snd |> Seq.map fst
    if badLines |> Seq.exists (fun _ -> true) then
        failwithf "bad lines found %A" (Array.ofSeq badLines)
    runProc cmd "nopause" None

type DbProjPathSpecifier = 
    |AbsoluteDbProjFolder of string
    |SlnPath of string
    //|Relative of string // not implemented
    // uses the magic of __SOURCE_DIRECTORY__ to locate the sln dir
    |SourceDirectoryIsBelowSln

//end environment purity section

let targetFolder = @"C:\VSDBCMD"
// sample uses:
//dbFolders |> Seq.head |> copyForDeploy;;
let copyForDeploy dbProjFolder =
    let deployFolder = @"C:\VSDBCMD\deployment"
    copyDbProjOutputs dbProjFolder deployFolder
    deployFolder

let getPmAppDbProjPath specifier = 
    let targetDbFolderName = "ApplicationDatabase"
    match specifier with
    | AbsoluteDbProjFolder s -> 
        if s.EndsWith(".dbproj") then failwithf "path should point to the folder containg the dbProj"
        s
    | SlnPath s -> 
        combine s targetDbFolderName
    | SourceDirectoryIsBelowSln ->
        combineAll [__SOURCE_DIRECTORY__ ;".." ;targetDbFolderName] |> Path.GetFullPath

let deployPmApplicationPath = @"C:\VSDBCMD\deployment\deployPmApplication.cmd"

let runPmApp dbProjFolderSpecifier = 
    let folder = getPmAppDbProjPath dbProjFolderSpecifier
    let deployFolder = folder |> buildDbProj None |> fst |> copyForDeploy
    let output,errors = runDeploy deployPmApplicationPath
    printfn "output: %A" (Array.ofSeq output)
    printfn "errors %A" (Array.ofSeq errors)

//this version calls vsdbcmd directly so we can capture the output errors and warnings
let runPmApp' dbProjSpecifier = 
    let cmdPath = @"C:\VSDBCMD\vsdbcmd.exe"
    let cmdArgs = """/a:Deploy /cs:"Server=.;Integrated Security=true" /dsp:Sql /dd+ /manifest:"C:\VSDBCMD\deployment\ApplicationDatabase.deploymanifest" /p:AlwaysCreateNewdatabase=False /p:PerformDatabaseBackup=False /p:IgnoreColumnCollation=True"""

    if File.Exists deployPmApplicationPath then //make sure the script and our args haven't drifted
        let text = File.ReadAllText deployPmApplicationPath
        assert (text.Contains cmdArgs)

    let folder = getPmAppDbProjPath dbProjSpecifier
    let deployFolder = folder |> buildDbProj None |> fst |> copyForDeploy
    let output,errors = runProc @"C:\VSDBCMD\vsdbcmd.exe" """/a:Deploy /cs:"Server=.;Integrated Security=true" /dsp:Sql /dd+ /manifest:"C:\VSDBCMD\deployment\ApplicationDatabase.deploymanifest" /p:AlwaysCreateNewdatabase=False /p:PerformDatabaseBackup=False /p:IgnoreColumnCollation=True""" (Some @"C:\VSDBCMD\")
    printfn "output: %A" (Array.ofSeq output)
    printfn "errors %A" (Array.ofSeq errors)

// sample usages:
// buildDbProj None @"C:\TFS\Pm-Rewrite\Source-dev-rewrite\PracticeManagement\ApplicationDatabase";;
// runPmApp' (AbsoluteDbProjFolder @"C:\TFS\Pm-Rewrite\Source-dev-rewrite\PracticeManagement\ApplicationDatabase");;