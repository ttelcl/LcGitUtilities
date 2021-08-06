module AppLog

open System
open System.IO

open LcGitLib
open LcGitLib.GitRunning
open LcGitLib.FileUtilities
open LcGitLib.RawLog

open CommonTools
open ArgumentLogger

type private LogOutput =
  | ToConsole
  | SaveFile of string
  | Formatted of string

type private LogOptions = {
  StartFolder: string
  Limit: int option
  RestArgs: string list
  RawMode: bool
  Destination: LogOutput
}

let runApp args =
  bequiet <- true
  let o = {
    StartFolder = null
    Limit = None
    RestArgs = []
    RawMode = false
    Destination = LogOutput.ToConsole
  }
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      bequiet <- false
      rest |> parseMore o
    //| "-q" :: rest ->
    //  bequiet <- true
    //  rest |> parseMore o
    | "-n" :: limit :: rest ->
      let n = limit |> Int32.Parse
      if n>=1 then
        rest |> parseMore {o with Limit = Some(n)}
      else
        rest |> parseMore {o with Limit = None}
    | "-raw" :: rest ->
      rest |> parseMore {o with RawMode = true}
    | "-C" :: folder :: rest ->
      let folder = folder |> RepoDb.resolveRepoWitness
      rest |> parseMore {o with StartFolder=folder}
    | "-save" :: filename :: rest ->
      let extension = Path.GetExtension(filename).ToLowerInvariant()
      let rawMode =
        match extension with
        | ".txt" -> o.RawMode
        | _ -> true
      if rawMode <> o.RawMode then
        color Color.Yellow
        eprintf "Forcing raw mode because of save file extension '%s'" extension
        resetColor()
        eprintfn "."
      rest |> parseMore {o with Destination = LogOutput.SaveFile(filename); RawMode = rawMode}
    | "-console" :: rest ->
      rest |> parseMore {o with Destination = LogOutput.ToConsole}
    | "-json" :: rest ->
      rest |> parseMore {o with Destination = LogOutput.Formatted(".mjson"); RawMode = true}
    | "-fmt" :: format :: rest ->
      let fmt = format.ToLowerInvariant()
      let fmt =
        if fmt.StartsWith("*.") then
          fmt.Substring(2)
        elif fmt.StartsWith(".") then
          fmt.Substring(1)
        else
          fmt
      let fmt =
        match fmt with
        | "mjson" | "jsonm" ->
          LogOutput.Formatted(".mjson")
        | "json" ->
          LogOutput.Formatted(".json")
        | "jsonl" | "ljson" ->
          LogOutput.Formatted(".jsonl")
        | "txt" ->
          LogOutput.Formatted(".txt")
        | x ->
          failwithf "Unrecognized -fmt option '%s'" x
      rest |> parseMore {o with Destination = fmt; RawMode = true}
    | "+" :: rest ->
      [] |> parseMore {o with RestArgs = rest}
    | [] ->
      o
    | x :: _ ->
      failwithf "Unrecognized argument at '%s'" x
  let o = args |> parseMore o
  let cmdHost = makeCommandHost true
  let cmd =
    cmdHost.NewCommand(true)
  let cmd =
    if o.StartFolder |> String.IsNullOrEmpty then
      cmd
    else
      cmd.WithFolder(o.StartFolder)
  let cmd = cmd.WithCommand("log")
  let cmd =
    if o.RawMode then
      cmd.AddPost("--pretty=raw", "--all")
    else
      cmd
  let cmd =
    match o.Limit with
    | Some(n) ->
      cmd.AddPost("-n", n.ToString())
    | None ->
      cmd
  for restArg in o.RestArgs do
    cmd.AddPost(restArg) |> ignore
  let exitCode =
    match o.Destination with
    | LogOutput.ToConsole ->
      cmd.RunToConsole(true)
    | LogOutput.SaveFile(saveFile) ->
      let transaction = FileWriteTransaction(saveFile)
      let exitCode =
        eprintf "Saving "
        color Color.DarkYellow
        eprintf "%s" transaction.FileName
        resetColor()
        eprintfn "."
        use w = transaction.OpenText()
        let sink = w.RawGitLogObserver(transaction.FileName)
        cmdHost.RunToLineObserver(cmd, sink, true)
      transaction.Commit()
      exitCode
    | LogOutput.Formatted(fmt) ->
      let sink = LineWritingObserver.ToStdOut().RawGitLogObserver(fmt);
      cmdHost.RunToLineObserver(cmd, sink, true)
  if not bequiet then
    if exitCode = 0 then
      bcolor Color.DarkBlue
    else
      bcolor Color.DarkMagenta
    eprintf " > Exit code was "
    if exitCode = 0 then
      color Color.Green
    else
      color Color.Red
    eprintf "%d" exitCode
    resetColor()
    eprintfn "."

  ()

