<Query Kind="FSharpProgram">
  
</Query>

open System.Linq
let dc = new TypedDataContext()

let rawQ = query{
		for bd in dc.Tbl_BuildDefinitions.ToArray() do 
	
	//.Where(x=>x.LastSystemBuildStartTime>DateTime.Now.AddMonths(-2)) // only for system initiated (Scheduled?) builds

		let lastBuild = dc.Tbl_Builds.Where(fun b ->b.DefinitionId=bd.DefinitionId).OrderByDescending(fun b->b.StartTime).FirstOrDefault()
		where (lastBuild <>null )
		select (bd, lastBuild) }
		
let GetMsBuildArgs (processParams:XElement) = 
	processParams.Elements().Where(fun e->e.Name.LocalName="String")
		.Where(fun e->e.Attributes().Any(fun a-> a.Name.LocalName = "Key" && a.Value="MSBuildArguments"))
		.Select(fun e->e.Value).FirstOrDefault()
		
type ProcessParametersPair = {Definition:string; Provided:string}
type QueryResult = {FinishTime:Nullable<DateTime>; DefinitionId:int;DefinitionName:string; ProcessTemplateId:int; StatusName:string; DefinitionArgs:string; BuildArgs:string;
	StatusCode:int option;cleanWorkspaceOption:string; RawProcessParameters:ProcessParametersPair; lastBuild:Tbl_Build}		
type QueryDisplay = { DefinitionId:int;DefinitionName:string; ProcessTemplateId:int; StatusName:Object; DefinitionArgs:string; BuildArgs:string;
	StatusCode:int option;cleanWorkspaceOption:string; RawProcessParameters:Object; lastBuild:Object}	

// F# query expressions http://msdn.microsoft.com/en-us/library/hh225374.aspx	
let q = query { 
	for (bd,lastBuild) in rawQ do
	let status = match lastBuild with
					| lastBuild when lastBuild<>null && lastBuild.BuildStatus.HasValue -> Some(lastBuild.BuildStatus.Value)
					| _ -> None
	let statusName = match status with 
					|None -> null
					|Some(1) -> "In Progress"
					|Some(8) -> "Failed"
					|Some(2) -> "Success"
					|_ -> status.ToString()
	let processParamsParsed= XElement.Parse(bd.ProcessParameters)
	let buildParamsParsed =if lastBuild.ProcessParameters=null then null else XElement.Parse(lastBuild.ProcessParameters)
	let cleanWorkspaceOption = processParamsParsed.Elements().Where(fun e -> e.Name.LocalName="CleanWorkspaceOption").Select(fun e -> e.Value).FirstOrDefault()
	let msBuildArgs = GetMsBuildArgs(processParamsParsed)
	let lastBuildArgs =if buildParamsParsed=null then null else GetMsBuildArgs(buildParamsParsed)
	sortBy statusName
	select {
		FinishTime=lastBuild.FinishTime
		DefinitionId=bd.DefinitionId;
		DefinitionName=bd.DefinitionName;
		ProcessTemplateId=bd.ProcessTemplateId;
		StatusName=statusName; //Util.HighlightIf(,fun x -> x<>"Success");
		DefinitionArgs=msBuildArgs;
		BuildArgs=lastBuildArgs;
		StatusCode =status;
		cleanWorkspaceOption=cleanWorkspaceOption;
		RawProcessParameters =  {Provided=lastBuild.ProcessParameters;Definition=bd.ProcessParameters};//Util.OnDemand("Process Parameters",fun () ->);
		lastBuild=lastBuild//Util.OnDemand("LastBuild",fun () ->)
		}
	}

q.Dump();