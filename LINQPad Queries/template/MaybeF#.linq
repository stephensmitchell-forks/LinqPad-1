<Query Kind="FSharpProgram">
  <Namespace>Microsoft.FSharp.Quotations</Namespace>
  <Namespace>Microsoft.FSharp.Quotations.DerivedPatterns</Namespace>
  <Namespace>Microsoft.FSharp.Quotations.Patterns</Namespace>
</Query>

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.Patterns

//http://www.contactandcoil.com/software/dotnet/getting-a-property-name-as-a-string-in-f/

type thing = 
    { First : int
      Second : int;Hello:string;HelloOption:string option }

let something = 
    Some { thing.First = 1
           Second = 2
		   Hello="hello"
		   HelloOption=Some("helloOption")
		   }

let somethingElse = 
	Some({something.Value with Hello=null;HelloOption=None})
let nothing = Option<thing>.None

let (|SomeObj|_|) = // http://stackoverflow.com/a/6290897/57883
                    
    let ty = typedefof<option<_>>
    fun (a : obj) -> 
        let aty = a.GetType()
        let v = aty.GetProperty("Value")
        if aty.IsGenericType 
           && aty.GetGenericTypeDefinition
                  () = ty then 
            if a = null then None
            else 
                Some
                    (v.GetValue(a, [||]))
        else None

let maybe (expr : Expr) : Option<_> = 
    let rec visit (expr : Expr) : Option<_> = 
        match expr with
        | Application(expr1, expr2) -> 
            printfn "application"
            //let val1 = 
            let exp1Result = visit expr1
            if exp1Result.IsSome then 
                let exp2Result = 
                    visit expr2
                if exp2Result.IsSome then 
                    //exp1Result.Value.GetType().Dump()
                    None
                //exp2Result.Value
                else None
            else None
        //|SpecificCall <@@ (+) @@> (_, _, exprList) ->
        //  printfn "%A"
        | Call(exprOpt, methodInfo, exprList) -> 
            // method or module function call
            printfn "call"
            let exprValOpt = 
                match exprOpt with
                | Some expr -> 
                    visit expr
                | None -> 
                    printfn "%s" 
                        methodInfo.DeclaringType.Name
                    None
            printfn ".%s(" 
                methodInfo.Name
            if exprList.IsEmpty then 
                printf ")"
                None
            else 
                visit exprList.Head
                //printfn "Call %A %A %A" exprOpt methodInfo exprList
                for expr in exprList.Tail do
                    printf ","
                    visit expr
                printf ")"
                None
        | Int32(n) -> 
            printfn "Int32 %d" n
            None
        | Lambda(param, body) -> 
            printfn "fun (%s:%s) -> " 
                param.Name 
                (param.Type.ToString())
            visit body
        | Let(var, expr1, expr2) -> 
            if var.IsMutable then 
                printf 
                    "let mutable %s = " 
                    var.Name
            else 
                printf "let %s = " 
                    var.Name
            visit expr1 |> ignore
            printf " in "
            visit expr2
        //      |PropertyGet(Some(instance), pi, args ) ->  //http://stackoverflow.com/questions/6403600/evaluate-function-inside-quotation
        //          printfn "Some!"
        | PropertyGet(expr1Opt, 
                      propOrValInfo, 
                      args) -> 
            match expr1Opt with
            | Some expr2 -> 
                //printfn  "visiting an expr inside a propertyGet %A %A" expr1Opt args
                let expr2Value = visit expr2
                match expr2Value with
                |Some(expr3) -> match expr3 with
                                | null -> printfn "Null1!";None
                                | SomeObj(t) -> printfn "found t! %A" t; None
                                | y when y.GetType() = propOrValInfo.DeclaringType -> 
                                    //printfn " yay types aligned %A" y; 
                                    let value = propOrValInfo.GetValue(y,null)
                                    Some(value)
                                | _ as x -> 
                                    //printfn "found _ %A with %A" x propOrValInfo
                                    //printfn "propDeclaring %A" propOrValInfo.DeclaringType
                                    //printfn "x type is %A" (x.GetType())
                                    Some(x)
                |_ as x -> None
            //let value = propOrValInfo.GetValue(expr2,null)
            | None -> 
                //printfn "PropertyGet None"
                //handle var access?
                let value = 
                    propOrValInfo.GetValue (null, null)
                match value with
                | null -> 
                    //printfn "Null2!"
                    None //None
                | SomeObj(x1) -> 
                    Some(x1)
        //(propOrValInfo,propOrValInfo.GetType(),args).Dump("pi")
        | String(str) -> 
            printf "string %s" str
            None
        | Var(var) -> 
            printf "var %s" var.Name
            None
        | _ -> 
            printf "unmatched %s" 
                (expr.ToString())
            None
    visit expr

printfn "%A" (maybe <@ something.Value.First @>)
printfn "%A" (maybe <@ something.Value.Second @>)
printfn "%A" (maybe <@ something.Value.Hello @>)
printfn "%A" (maybe <@ something.Value.HelloOption @>)
printfn "\r\n and now something else \r\n"
printfn "%A" (maybe <@ somethingElse.Value.First @>)
printfn "%A" (maybe <@ somethingElse.Value.Second @>)
printfn "%A" (maybe <@ somethingElse.Value.Hello @>)
printfn "%A" (maybe <@ somethingElse.Value.HelloOption @>)
printfn "\r\n and now nothing \r\n"
printfn "%A" (maybe <@ nothing.Value.First @>)
//printfn "\r\n\r\n%A" <@ something.Value.First @>
//printfn "\r\n\r\n%A" <@ nothing.Value.First @>