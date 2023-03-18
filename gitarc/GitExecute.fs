module GitExecute

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


