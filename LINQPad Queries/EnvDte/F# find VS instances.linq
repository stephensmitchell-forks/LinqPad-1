<Query Kind="FSharpProgram">
  <Reference>&lt;CommonProgramFiles&gt;\Microsoft Shared\MSEnv\PublicAssemblies\envdte.dll</Reference>
  <Reference>&lt;CommonProgramFiles&gt;\Microsoft Shared\MSEnv\PublicAssemblies\envdte80.dll</Reference>
</Query>

//let why =
//	http://apavan.net/wordpress/?p=41
open System;
open System.Runtime.InteropServices;
open System.Runtime.InteropServices.ComTypes;
open EnvDTE;
open System.Diagnostics;
//http://stackoverflow.com/questions/10864595/getting-the-current-envdte-or-iserviceprovider-when-not-coding-an-addin

//http://stackoverflow.com/questions/6558789/how-to-convert-out-ref-extern-parameters-to-f
//http://stackoverflow.com/questions/1689460/f-syntax-for-p-invoke-signature-using-marshalas

[<System.Runtime.InteropServices.DllImport("ole32.dll")>] 
extern int CreateBindCtx(System.IntPtr inRef, IBindCtx& outParentRef);
[<System.Runtime.InteropServices.DllImport("ole32.dll")>]
extern int GetRunningObjectTable(System.IntPtr inRef, IRunningObjectTable& outParentRef);
//let dte = System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.12.0") :?> EnvDTE80.DTE2
let comName="VisualStudio.DTE.12.0"
let rotEntry = "!"+comName
//let mutable rot:IRunningObjectTable =null

let rot=
	let mutable result:IRunningObjectTable = null
	GetRunningObjectTable(nativeint 0, &result) |> ignore
	result


let mutable enumMoniker:IEnumMoniker = null
rot.EnumRunning (&enumMoniker) 

enumMoniker.Reset() |> ignore
let mutable fetched = IntPtr.Zero
let mutable moniker:IMoniker[] = Array.zeroCreate 1 //http://msdn.microsoft.com/en-us/library/dd233214.aspx

let matches = seq {
	while enumMoniker.Next(1, moniker, fetched) = 0 do
		"looping" |> Dump
		for i in moniker do
			"inner looping" |> Dump
			let mutable bindCtx:IBindCtx = null
			CreateBindCtx(nativeint 0, &bindCtx) |> ignore
			let mutable displayName:string = null
			moniker.[0].GetDisplayName(bindCtx,null, &displayName)
			displayName |> Dump
			if displayName.StartsWith(rotEntry) then
				let mutable comObject = null
				rot.GetObject(moniker.[0], &comObject) |> ignore
				let dte =  comObject:?>EnvDTE80.DTE2
				yield displayName,bindCtx,comObject,dte.FullName, dte
			else
				Marshal.ReleaseComObject(bindCtx) |> ignore
	Marshal.ReleaseComObject(enumMoniker) |> ignore
}
matches |> Dump
Marshal.ReleaseComObject(rot) |> ignore