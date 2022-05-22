module AppClone

open System.IO

open HyperGitLib
open LcGitLib.GitRunning

open ColorPrint
open CommonTools

type private CloneOptions = {
  CfgFile: string
  Execute: bool
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
      rest |> parseMore o
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
        if verbose then
          cp $"\fk{e.Name}\f0 (exists)"
      else
        missing <- missing + 1
        cp $"\fg{e.Name}\f0:"
        if e.BucketPath |> Directory.Exists |> not then
          cp $"  Creating \fy{e.BucketPath}\f0"
          Directory.CreateDirectory(e.BucketPath) |> ignore
        let needName =
          e.Renamed
        let bucket = e.BucketPath |> gitpath
        let cmd =
          if githost <> null then
            githost
              .NewCommand()
              .WithFolder(bucket)
              .WithCommand("clone")
              .AddPost("--mirror")
              .AddPost("--no-hardlinks")
              .AddPost(e.Origin)
          else
            null
        let cmd =
          if needName then
            cp $"  \fwgit -C {bucket} clone --mirror --no-hardlinks {e.Origin} {e.Name}.git\f0"
            if cmd <> null then
              cmd.AddPost(e.Name + ".git")
            else
              cmd
          else
            cp $"  \fwgit -C {bucket} clone --mirror --no-hardlinks {e.Origin}\f0"
            cmd
        if cmd <> null then
          let status = cmd.RunToConsole(true)
          if status <> 0 then
            failwith $"GIT failed with status {status}"
    else
      ignored <- ignored + 1
      if verbose then
        cp $"\fk%35s{e.Name}  (inactive)"
  let total = existing + missing
  cp $"Of \fc{total}\f0 total active repositories, \fg{missing}\f0 are missing, and \fy{existing}\f0 already exist"
  0

