<Query Kind="Expression" />

// status reverted to WIP:
// where did the s.Database_id column go?


//SELECT s.login_name,d.name as dbName, 
//req.command,
//
//
//sqltext.TEXT,
//s.program_name,
//req.session_id,
//req.status,
//
//req.cpu_time,
//req.total_elapsed_time,
//req.transaction_id,
//s.host_name,
//s.client_interface_name,
//s.memory_usage,
//s.database_id


 (from req in sys.Dm_exec_requests
 	let sqltext = sys.dm_exec_sql_text(req.Sql_handle)
	join sl in sys.Dm_exec_sessions on req.Session_id equals sl.Session_id into sLeft
	from s in sLeft.DefaultIfEmpty()
	join dl in sys.Databases on s.Database_id equals dl.Database_id into dLeft
	from d in dLeft.DefaultIfEmpty()
	where d==null || d.Name==this.Connection.Database
	
	select new {s.Login_name,s.Original_login_name,s.Program_name,
		Kill=req.Command=="KILLED/ROLLBACK" || req.Session_id<=50?null: new LINQPad.Hyperlinq( QueryLanguage.SQL, "kill "+req.Session_id,"kill"),
		sqltext,req,s,d
		})
		.ToArray().OrderByDescending (s => s.sqltext.Any ())