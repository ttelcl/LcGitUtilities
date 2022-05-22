module GitUtilCfg

open System
open System.Diagnostics
open System.IO

open Newtonsoft.Json

open LcGitLib.GitRunning
open LcGitLib.MetaBase

open CommonTools

let mutable bequiet = false

let private printArguments shortGit (psi:ProcessStartInfo) =
  if not bequiet then
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
  ()

let makeLogger shortGit =
  let gal = new GitArgLogger(fun psi -> printArguments shortGit psi)
  gal

let loadConfig saveIfMissing =
  let cfgFile = LcGitConfig.ConfigFile
  if File.Exists cfgFile then
    if bequiet |> not then
      eprintf "Loading configuration file "
      color Color.Green
      eprintf "%s" cfgFile
      resetColor()
      eprintfn "."
    LcGitConfig.LoadDefault()
  else
    let cfg =
      if saveIfMissing then
        bcolor Color.Yellow
        color Color.Black
        eprintf "Generating default configuration file - please edit the missing parts"
        resetColor()
        eprintfn "."
        let cfg = LcGitConfig.LoadDefault(false)
        let json = JsonConvert.SerializeObject(cfg, Formatting.Indented)
        do
          let folder = Path.GetDirectoryName(cfgFile)
          if folder |> Directory.Exists |> not then
            eprintf "Creating folder "
            color Color.Cyan
            folder |> eprintf "%s"
            resetColor()
            eprintfn "."
            folder |> Directory.CreateDirectory |> ignore
          use w = cfgFile |> startFile
          w.WriteLine(json)
        cfgFile |> finishFile
        cfg
      else
        eprintf "The default configuration file is missing: "
        color Color.DarkYellow
        eprintf "%s" cfgFile
        resetColor()
        eprintf " Use '"
        color Color.Yellow
        eprintf "lcgitutil cfg -init"
        resetColor()
        eprintf " to generate a stub, and then edit it."
        eprintfn "."
        failwith "Aborting"
    cfg

let makeCommandHost shortGit =
  new GitCommandHost(makeLogger shortGit, loadConfig false)

let lazyConfig = lazy (loadConfig false)

let lazyStage = lazy (new Stage(lazyConfig.Force()))

let lazyRepos = lazy (lazyStage.Force().LoadRepoDb(UpdateLevel.Update))
