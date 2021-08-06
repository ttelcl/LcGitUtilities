module AppRepocfg


open System
open System.IO

open Newtonsoft.Json

open LcGitLib
open LcGitLib.GitConfigFiles
open LcGitLib.RepoTools

open CommonTools
open PrintUtils

type private RepoJsonFormat =
  | NotJson
  | Full
  | Compact
  | ExtraCompact

type private RepoCfgCommand =
  | Dump
  | Summary

type private RepoCfgOptions = {
  Anchor: string
  Command: RepoCfgCommand
  JsonFormat: RepoJsonFormat
}

let runRepocfg args =
  let o = {
    Anchor = Environment.CurrentDirectory
    Command = RepoCfgCommand.Summary
    JsonFormat = RepoJsonFormat.NotJson
  }
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-dump" :: rest ->
      let fmt =
        match o.JsonFormat with
        | RepoJsonFormat.NotJson ->
          RepoJsonFormat.Full
        | x ->
          x
      rest |> parseMore {o with Command = RepoCfgCommand.Dump; JsonFormat = fmt}
    | "-summary" :: rest ->
      rest |> parseMore {o with Command = RepoCfgCommand.Summary; JsonFormat = RepoJsonFormat.NotJson}
    | "-compact" :: rest ->
      let cmd =
        match o.Command with
        | RepoCfgCommand.Summary ->
          RepoCfgCommand.Dump
        | x ->
          x
      rest |> parseMore {o with Command = cmd; JsonFormat = RepoJsonFormat.Compact}
    | "-extracompact" :: rest ->
      let cmd =
        match o.Command with
        | RepoCfgCommand.Summary ->
          RepoCfgCommand.Dump
        | x ->
          x
      rest |> parseMore {o with Command = cmd; JsonFormat = RepoJsonFormat.ExtraCompact}
    | "-C" :: anchor :: rest ->
      let anchor = anchor |> RepoDb.resolveRepoWitness
      rest |> parseMore {o with Anchor = anchor}
    | [] ->
      o
    | x :: _ ->
      failwithf "Unrecognized argument at: %s" x
  let o =
    args |> parseMore o
  let cfgfile = RepoUtilities.FindGitConfigFile(o.Anchor)
  if cfgfile = null then
    failwithf "No repo or config found near %s" o.Anchor
  printf "Loading "
  color Color.Green
  printf "%s" cfgfile
  resetColor()
  printfn "."
  let cfg = GitConfig.FromFile(cfgfile)
  printfn "Load complete:"
  let formatJson () =
    match o.JsonFormat with
    | RepoJsonFormat.NotJson ->
      failwith "Asking for JSON for a non-JSON command"
    | RepoJsonFormat.Full ->
      JsonConvert.SerializeObject(cfg, Formatting.Indented)
    | RepoJsonFormat.Compact ->
      let jo = cfg.ToCompactJson(true)
      JsonConvert.SerializeObject(jo, Formatting.Indented)
    | RepoJsonFormat.ExtraCompact ->
      let jo = cfg.ToCompactJson(false)
      JsonConvert.SerializeObject(jo, Formatting.Indented)
  match o.Command with
  | RepoCfgCommand.Dump ->
    let json = formatJson ()
    color Color.White
    printf "%s" json
    resetColor()
    printfn "\nDone!"
  | RepoCfgCommand.Summary ->
    cfg.IsBare |> string |> lp "Bare"
    let m = cfg.RemoteUrls()
    for kvp in m do
      let label = sprintf "remote:%s" kvp.Key
      kvp.Value |> lp label
    if m.Count = 0 then
      color Color.DarkYellow
      printf "There are no remotes"
      resetColor()
      printfn "."

  ()

