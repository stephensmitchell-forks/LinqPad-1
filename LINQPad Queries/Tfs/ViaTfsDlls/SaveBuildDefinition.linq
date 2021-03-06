<Query Kind="Program">
  <Reference>&lt;ProgramFilesX86&gt;\Microsoft Visual Studio 12.0\Common7\IDE\ReferenceAssemblies\v2.0\Microsoft.TeamFoundation.Build.Client.dll</Reference>
  <Reference>&lt;ProgramFilesX86&gt;\Microsoft Visual Studio 12.0\Common7\IDE\ReferenceAssemblies\v2.0\Microsoft.TeamFoundation.Client.dll</Reference>
  <Reference>&lt;ProgramFilesX86&gt;\Microsoft Visual Studio 12.0\Common7\IDE\ReferenceAssemblies\v2.0\Microsoft.TeamFoundation.VersionControl.Client.dll</Reference>
  <Reference>C:\projects\Fsi\tfsmacros.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Macros</Namespace>
  <Namespace>Microsoft.TeamFoundation.Build.Client</Namespace>
  <Namespace>Microsoft.TeamFoundation.Client</Namespace>
  <Namespace>Microsoft.TeamFoundation.VersionControl.Client</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

//http://stackoverflow.com/questions/2909416/how-can-i-copy-a-tfs-2010-build-definition
void Main()
{
//unfinished? https://gist.github.com/jstangroome/6747950
#if UseFsi
var tfs = TFS.GetTfs();
#else
var tfs = Macros.TfsModule.GetTfs(new Uri("http://tfs20102:8080/tfs"));
#endif

	var teamProjects = new[] {
		new { Name="PracticeManagement", BuildDefinitionPath =@"C:\TFS\Pm-Rewrite\Source-dev-rewrite\BuildDefinitionBackups"},
		new { Name="XpressCharts", BuildDefinitionPath =@"C:\tfs\XpressCharts\BuildDefinitionBackups"},
	};
	
	var build =new TfsBuild( Build.GetBuildServer(tfs));
	
	var teamProjectName =Util.ReadLine("Project?",teamProjects[0], teamProjects);
	
	
	
	var builds = build.GetBuildDefinitions(teamProjectInfo.);
	// var tp = vcs.GetTeamProjectForServerPath("$/Development");
	var teamProjectCollectionUri = Build.GetBuildServer(tfs).TeamProjectCollection.Uri;
	var buildToSave=Util.ReadLine("Build?","PracticeManagementRW",builds.Select (b => b.Name).Dump("options") );
	
	var buildToDump=build.GetBuildDefinition(teamProjectInfo.TeamProjectName, buildToSave);
	
	var buildDefinition= new {
			Uri=buildToDump.Uri, Id=buildToDump.Id, TeamProject=buildToDump.TeamProject,
			CollectionUri=teamProjectCollectionUri,
			Name=buildToDump.Name,
			Description=buildToDump.Description,
			QueueStatus= buildToDump.QueueStatus,
			Trigger= new{
				TriggerType=buildToDump.TriggerType,
				ContinuousIntegrationType=buildToDump.ContinuousIntegrationType,
				ContinuousIntegrationQuietPeriodMinutes = buildToDump.ContinuousIntegrationQuietPeriod,
				BatchSize= buildToDump.BatchSize
			},
			Schedules = buildToDump.Schedules.Select (s =>new{DaysToBuild=s.DaysToBuild, StartTimeSecondsPastMidnight=s.StartTime, TimeZone = s.TimeZone, Type=s.Type} ).ToArray(),
			Workspace =new{ Mappings= buildToDump.Workspace.Mappings.Select (m =>new{m.MappingType, m.ServerItem, m.LocalItem, m.Depth}).ToArray()},
			BuildController = new{buildToDump.BuildControllerUri,buildToDump.BuildController.Name, buildToDump.BuildController.Description, buildToDump.BuildController.CustomAssemblyPath},
			buildToDump.DefaultDropLocation,
			Process= new {buildToDump.Process.Id, buildToDump.Process.Description,buildToDump.Process.ServerPath, buildToDump.Process.SupportedReasons, buildToDump.Process.TeamProject,
				buildToDump.Process.TemplateType, buildToDump.Process.Version
			},
			ProcessParameters= buildToDump.ProcessParameters,
			RetentionPolicies = buildToDump.RetentionPolicyList.Select (rpl => new{ rpl.BuildReason, rpl.BuildStatus, rpl.DeleteOptions, rpl.NumberToKeep}).ToArray()
		};
	
	buildDefinition.Dump("serialized?");
	var json=Newtonsoft.Json.JsonConvert.SerializeObject(buildDefinition,Newtonsoft.Json.Formatting.Indented).Dump();
	var targetPath = System.IO.Path.Combine(saveDir,buildDefinition.Name+".json");
	if(System.IO.Directory.Exists(saveDir)==false)
	{
		//throw new DirectoryNotFoundException(saveDir);
		System.IO.Directory.CreateDirectory(saveDir);
	}
	System.IO.File.WriteAllText(targetPath, json);
	targetPath.Dump("saved to");
}