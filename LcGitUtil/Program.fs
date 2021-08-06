// (c) 2021  VTT / TTELCL

open System
open System.IO

open CommonTools
open ExceptionTool
open PrintUtils
open Usage

let private initcfg doInit =
  let cfg = ArgumentLogger.loadConfig doInit
  cfg.GitPath     |> lp "GitPath"
  cfg.MachineName |> lp "Machine Name"
  cfg.StageFolder |> lp "Stage Root"
  cfg.UsbFolder   |> lp "USB Root"
  ()

let rec run arglist =
  match arglist with
  | "-v" :: rest ->
    verbose <- true
    rest |> run
  | "log" :: rest ->
    rest |> AppLog.runApp
  | "rawlog" :: rest ->
    ("-raw" :: rest) |> AppLog.runApp
  | "newest" :: rest ->
    ("-n" :: "1" :: "-json" :: rest) |> AppLog.runApp
  | "findrepo" :: rest ->
    rest |> AppLabel.runLabel
  | "recipe" :: recipe :: rest ->
    rest |> AppRecipe.runRecipe recipe
  | "recipe" :: [] ->
    AppRecipe.recipeUsage()
  | "cfg" :: "-init" :: [] ->
    initcfg true
  | "cfg" :: "-check" :: [] ->
    initcfg false
  | "db" :: rest ->
    rest |> AppDb.runDb
  | "repocfg" :: rest ->
    rest |> AppRepocfg.runRepocfg
  | "graph" :: rest ->
    rest |> AppGraph.runGraph
  | x :: _ ->
    failwithf "Unrecognized argument at '%s'" x
  | [] ->
    usage()
  ()

[<EntryPoint>]
let main args =
  try
    match args.Length with
    | 0 ->
      usage()
    | _ ->
      args |> Array.toList |> run
  with
    | ex ->
      ex |> fancyExceptionPrint verbose
      resetColor ()
  0



