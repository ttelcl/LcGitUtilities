module AppList

open CommonTools
open ColorPrint
open System.IO
open HyperGitLib

type private ListOptions = {
  CfgFile: string
}

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-f" :: cfgfile :: rest ->
      rest |> parseMore {o with CfgFile = cfgfile}
    | [] ->
      o
    | x :: _ ->
      failwithf $"Unrecognized argument '{x}'"
  let o = args |> parseMore {
    CfgFile = "repos.csv"
  }
  let hgc = new HyperGitConfig(o.CfgFile)
  cp $"Loading configuration from \fg{hgc.FileName}\f0"
  for e in hgc.Entries do
    if e.IsEnabled then
      cpx $"\fg%35s{e.Name}\f0 "
      let repopath = e.RepoPath
      if repopath |> Directory.Exists then
        cp $"\fc{repopath}\f0"
      else
        cp $"\fk{repopath}\f0"
    else
      if e.HostMatch || e.HostDisabled then
        cp $"\fk%35s{e.Name} (disabled)"
      else
        if verbose then
          cp $"\fk%35s{e.Name} (not active for this host)"
  0
