module GitExecute

open System
open System.Diagnostics
open System.IO

open LcGitLib.GitRunning

open ColorPrint
open CommonTools

let private printArguments shortGit (psi:ProcessStartInfo) =
  let gitName =
    if shortGit then
      Path.GetFileName(psi.FileName)
    else
      psi.FileName
  bcolor Color.DarkBlue
  eprintf " > "
  color Color.DarkYellow
  eprintf "%s" gitName
  let mutable phase = true
  for gitArg in psi.ArgumentList do
    if phase then
      color Color.Green
    else
      color Color.Cyan
    phase <- not phase
    eprintf " %s" gitArg
  resetColor()
  eprintfn ""

let makeGitHost echo shortGit =
  let gal =
    if echo then new GitArgLogger(fun psi -> printArguments shortGit psi) else null
  new GitCommandHost(gal, null)

let lazyGitHost = lazy (makeGitHost verbose true)

let gitCommand command repoFolder =
  lazyGitHost.Force()
    .NewCommand()
    .WithFolder(repoFolder |> Option.defaultValue null)
    .WithCommand(command)

let cmdAddPost1 value (cmd: GitCommand) =
  cmd.AddPost1 value

let gitLsRemoteCommand repoFolder remoteName =
  let cmd = gitCommand "ls-remote" repoFolder
  let cmd = cmd.AddPost("--refs")
  match remoteName with
  | Some(name) -> 
    cmd.AddPost1(name)
  | None ->
    cmd

let gitShowRefCommand repoFolder =
  let cmd = gitCommand "show-ref" repoFolder
  cmd

let gitUpdateRefCommand repoFolder source target =
  let cmd = gitCommand "update-ref" repoFolder
  cmd 
  |> cmdAddPost1 target
  |> cmdAddPost1 source

