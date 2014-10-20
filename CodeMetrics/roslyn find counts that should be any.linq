<Query Kind="FSharpProgram">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>ArchiMetrics.Analysis</NuGetReference>
  <Namespace>Roslyn.Compilers</Namespace>
  <Namespace>Roslyn.Compilers.Common</Namespace>
  <Namespace>Roslyn.Compilers.CSharp</Namespace>
  <Namespace>Roslyn.Services</Namespace>
  <Namespace>Roslyn.Services.Editor</Namespace>
</Query>

// find all ifs that have the return on the same line
// http://blog.filipekberg.se/2011/10/20/using-roslyn-to-parse-c-code-files/
type StringTransform = (string->string)
type SearchOptions =
	|Absolute of string
	|RelativeFile of StringTransform
	|AbsoluteDir of string
	|UseEnvironmentalBase
	|RelativeDir of StringTransform
	|AskForAbsoluteDir of (unit-> string)

//adjust your preference here
let analysisSearchOption = SearchOptions.UseEnvironmentalBase
let devRoot = Environment.ExpandEnvironmentVariables("%devroot%")
let slnPath = 
	System.IO.Directory.GetFiles(devRoot,"*.sln",SearchOption.AllDirectories)
	|> Seq.skipWhile(fun d->d.Contains("Playground")=false)
	|> Seq.skipWhile(fun d->d.Contains("Playground"))
	|> Seq.head
	|> fun e-> e.Dump("sln file"); e

	
// active pattern : http://stackoverflow.com/a/25243799/57883
let (|As|_|) (p:'T) : 'U option =
    let p = p :> obj
    if p :? 'U then Some (p :?> 'U) else None	
	
let solution:ISolution =
	try
		Solution.Load(slnPath)
	with ex -> 
		if ex.Message = "Expected Global line." then 
			"check for a line like this in your .sln \"VisualStudioVersion = \" or \"MinimumVisualStudioVersion = \"".Dump()
			LINQPad.Hyperlinq("https://social.msdn.microsoft.com/Forums/en-US/df9af25e-75fc-47e6-9704-8aa89817857c/systemexception-expected-global-line", "help topic").Dump()
			
		reraise()

let isCallToEnumerableCount (document:IDocument, expr:ExpressionSyntax, cancellationToken) = 
	let invocation = match expr with
						| As (i:InvocationExpressionSyntax) -> Some i
						| _ -> None
	if invocation.IsNone then
		false
	else
		let call = match invocation.Value with 
					| As( m:MemberAccessExpressionSyntax) -> Some m
					| _ -> None
		if call.IsNone then
			false
		else
			let semanticModel = document.GetSemanticModel(cancellationToken)
			let methodSymbol = semanticModel.GetSymbolInfo(expr, cancellationToken)
			let ms = methodSymbol.Symbol :?> MethodSymbol
			// printfn "checking in semantic with symbols"
			if ms = null || ms.Name <> "Count" || ms.ConstructedFrom = null then
				false
			else
				printfn "checking enumerable!"
				let enumerable = semanticModel.Compilation.GetTypeByMetadataName(typeof<Enumerable>.FullName)
				enumerable <> null && ms.ConstructedFrom.ContainingType.Equals(enumerable)
				
let isRelevantComparison (document:IDocument, expression:ExpressionSyntax, validator:int->bool,token) =
	let semantic = document.GetSemanticModel(token)
	let symbolInfo = semantic.GetSymbolInfo(expression)
	let constant = semantic.GetConstantValue(expression)
	if not constant.HasValue then false
	else
		let value = constant.Value :?> int
		validator(value)

let isRelevantRightSideComparison(document:IDocument, expression:ExpressionSyntax, kind,token) =
	isRelevantComparison(
			document, expression, 
			( fun v -> kind = SyntaxKind.GreaterThanExpression && v = 0 || kind = SyntaxKind.GreaterThanOrEqualExpression && v = 1),
			token
		)
				
let isRelevantLeftSideComparison(document:IDocument, expression:ExpressionSyntax, kind,token) =
	isRelevantComparison(
		document,
		expression,
		(fun v -> kind = SyntaxKind.LessThanExpression && v = 0 || kind = SyntaxKind.LessThanOrEqualExpression && v = 1),
		token
	)
	
let getIssues(document:IDocument, node:CommonSyntaxNode, token:CancellationToken) =
	let binaryExpression = node :?> BinaryExpressionSyntax
	let left,right,kind = binaryExpression.Left, binaryExpression.Right, binaryExpression.Kind
	[
		if isCallToEnumerableCount(document, left, token) && isRelevantRightSideComparison(document, right, kind, token) ||
			isCallToEnumerableCount(document,right,token) && isRelevantLeftSideComparison(document,left,kind,token) then
				yield CodeIssue(CodeIssueKind.Info, binaryExpression.Span,sprintf "Change %A to use Any() instead of Count() to avoid possible enumeration of entire sequence." binaryExpression)
	]
	
type IssueInformation = {ProjectName:string; DocumentName:string;IssueNodes:CodeIssue seq}				
			
let pData = 
	solution.Projects
	|> Seq.map (fun p-> 
		p.Documents 
		|> Seq.map (fun d ->
			let semantic = d.GetSemanticModel()
			let tree = semantic.SyntaxTree
			d,semantic,tree,(tree.GetRoot().DescendantNodes() 
				|> Seq.filter (fun x -> x :? BinaryExpressionSyntax) 
				|> Seq.cast<BinaryExpressionSyntax>)
			)
		|> Seq.filter (fun (_,_,_,nodes) -> Seq.exists ( fun e -> true) nodes)
		|> Seq.map (fun (d,semantic,tree,nodes) ->
			{ProjectName=p.Name;DocumentName=d.Name; IssueNodes = 
			//p.Name,d.Name,semantic,tree,// nodes, 
				[ 
					for i in nodes |> Seq.map (fun n->  getIssues(d, n,CancellationToken())) do
					yield! i
				]
			}
			)
		|> Seq.filter (fun (* (pname,dname,semantic,tree,  nodes, *) issueInfo  -> (issueInfo.IssueNodes |> Seq.exists( fun e-> true)))
		)
	|> Seq.filter (fun (* (pname, dname, semantic, tree, nodes, *) issueNodes -> (issueNodes |> Seq.exists( fun e -> true)))
	
pData |> Seq.head |> Dump

	

			
let replace t (r:string) (str:string) = str.Replace(t,r)
let showNewLines text =
	text 
	|> replace "\r" "\\r"
	|> replace "\n" "\\n"