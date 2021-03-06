<Query Kind="Statements">
  <Reference>&lt;ProgramFilesX86&gt;\Microsoft Visual Studio 12.0\Common7\IDE\ReferenceAssemblies\v2.0\Microsoft.TeamFoundation.Client.dll</Reference>
  <Reference>&lt;ProgramFilesX86&gt;\Microsoft Visual Studio 12.0\Common7\IDE\ReferenceAssemblies\v2.0\Microsoft.TeamFoundation.VersionControl.Client.dll</Reference>
  <Reference>C:\projects\Fsi\tfsmacros.dll</Reference>
  <GACReference>Microsoft.TeamFoundation.WorkItemTracking.Client, Version=12.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a</GACReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Microsoft.TeamFoundation.Client</Namespace>
  <Namespace>Microsoft.TeamFoundation.VersionControl.Client</Namespace>
  <Namespace>Microsoft.TeamFoundation.WorkItemTracking.Client</Namespace>
  <Namespace>Macros</Namespace>
</Query>

var tfs = new TFS();
var workItemStore = tfs.Tfs.GetService<WorkItemStore>();
const int ActualHours = 10100;
var projectName = "Development";
var variables = new Dictionary<string,string>(){ {"project",projectName}};
var workItemsAssignedToMe = workItemStore.Query("select [System.Id], [System.AreaPath], [System.Title], [System.State], [OceansideTen.ReportedBy] from WorkItems where [System.TeamProject] = @project and [System.State] <> 'Closed' and [System.State] <> 'Resolved' and [System.AssignedTo] = @me order by [System.State], [System.ChangedDate] desc",variables);
workItemsAssignedToMe.Count.Dump("Work items");
var changedHours = 
	workItemsAssignedToMe
	.Cast<WorkItem>()
	.Where(wi=>wi.Revisions
		.Cast<Revision>()
		.Any(r=>r.Fields.TryGetById(ActualHours)!=null && r.Fields.TryGetById(ActualHours).IsChangedInRevision))
		//.Dump()
		;

var display = 
	from chwi in changedHours
	from revision in chwi.Revisions.Cast<Revision>()
	let actualHoursField = revision.Fields.TryGetById(ActualHours)
	where actualHoursField !=null && actualHoursField.Value !=null
	
	let revisionDateField = revision.Fields.TryGetById(-5)
	where revisionDateField  !=null && actualHoursField.Value!=null
	
	let revisionDate =  (revisionDateField.Value as DateTime?)
	let actualHours = (int) actualHoursField.Value
	let originalHours = ((int?) actualHoursField.OriginalValue).GetValueOrDefault()
	let worked = actualHours - originalHours
	where worked >0 && revisionDate < new DateTime(2100,1,1) // some dates were 1/1/9999
	
	orderby revisionDate descending
	select new {chwi.Id, chwi.Title, chwi.AreaId, chwi.AreaPath,chwi.IterationId, chwi.IterationPath, Worked=worked, 
		RevisionDate = revisionDate, Day = revisionDate.Value.DayOfWeek};
				
display.Count().Dump("working revisions");			

var dic= new Dictionary<string,DateTime>{
		{"today",DateTime.Today},
		{"last24Hours",DateTime.Today.AddDays(-1)},
		{"lastWeek",DateTime.Now.StartOfWeek( DayOfWeek.Monday).AddDays(-7)},
		{"last30Days",DateTime.Today.AddDays(-30)}
	};
	
var q= from d in display
		from timeframe in dic.Keys
		where d.RevisionDate > dic[timeframe]
		group d by timeframe into g
		select g;
		
q.Dump();