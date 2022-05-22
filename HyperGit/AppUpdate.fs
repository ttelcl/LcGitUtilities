module AppUpdate

open System.IO

open HyperGitLib

open ColorPrint
open CommonTools

type private UpdateOptions = {
  CfgFile: string
  Execute: bool
  GitVerbose: bool
}

let gitpath (path:string) =
  if path.Contains(' ') then
    "\"" + path.Replace('\\', '/') + "\""
  else
    path.Replace('\\', '/')    

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore {o with GitVerbose = true}
    | "-V" :: rest ->
      rest |> parseMore {o with GitVerbose = true}
    | "-f" :: cfgfile :: rest ->
      rest |> parseMore {o with CfgFile = cfgfile}
    | "-x" :: rest ->
      rest |> parseMore {o with Execute = true}
    | [] ->
      o
    | x :: _ ->
      failwithf $"Unrecognized argument '{x}'"
  let o = args |> parseMore {
    CfgFile = "repos.csv"
    Execute = false
    GitVerbose = false
  }
  let hgc = new HyperGitConfig(o.CfgFile)
  let mutable ignored = 0
  let mutable existing = 0
  let mutable missing = 0
  let githost =
    if o.Execute then
      GitExecute.makeGitHost true
    else
      null
  for e in hgc.Entries do
    if e.IsEnabled then
      let repopath = e.RepoPath
      if repopath |> Directory.Exists then
        existing <- existing + 1
        cp $"\fg{e.Name}\f0:"
        let repo = repopath |> gitpath
        let cmd =
          if githost <> null then
            githost
              .NewCommand()
              .WithFolder(repopath)  // GitHost takes care of quoting
              .WithCommand("remote")
              .AddPostIf(o.GitVerbose, "-v")
              .AddPost("update", e.Remote)
          else
            null
        let gitverbose = if o.GitVerbose then " -v" else ""
        cp $"  \fwgit -C {repo} remote{gitverbose} update {e.Remote}\f0"
        if cmd <> null then
          let status = cmd.RunToConsole(true)
          if status <> 0 then
            failwith $"GIT failed with status {status}"
      else
        missing <- missing + 1
        if verbose then
          cp $"\fk{e.Name}\f0 (missing)"
    else
      ignored <- ignored + 1
      if verbose then
        cp $"\fk%s{e.Name}  (inactive)"
  let total = existing + missing
  cp $"Of \fc{total}\f0 total active repositories, \fg{missing}\f0 are still missing, and \fy{existing}\f0 exist"
  0


