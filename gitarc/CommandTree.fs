module CommandTree

open System
open System.IO

open Newtonsoft.Json

open LcGitLib.GitModels
open LcGitLib.GitModels.Refs

open ColorPrint
open CommonTools

type private Options = {
  RepoFolder: string option
  Remote: string option
}

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-C" :: folder :: rest ->
      rest |> parseMore {o with RepoFolder = Some(folder)}
    | "-R" :: remote :: rest ->
      rest |> parseMore {o with Remote = Some(remote)}
    | "-r" :: rest ->
      rest |> parseMore {o with Remote = Some("origin")}
    | "-h" :: _
    | "-help" :: _ ->
      Usage.usage "tree"
      None
    | [] ->
      Some(o)
    | x :: _ ->
      failwith $"Unrecognized argument: %s{x}"
  let oo = 
    args |> parseMore {
      RepoFolder = None
      Remote = None
    }
  match oo with
  | None ->
    1
  | Some(o) ->
    let host = GitExecute.makeGitHost verbose true
    let cmd = 
      match o.Remote with
      | Some(remote) ->
        host.NewCommand()
          .WithFolder(o.RepoFolder |> Option.defaultValue null)
          .WithCommand("ls-remote")
          .AddPost("--head")
          .AddPostIf(not(String.IsNullOrEmpty(remote)), remote)
      | None ->
        host.NewCommand()
          .WithFolder(o.RepoFolder |> Option.defaultValue null)
          .WithCommand("show-ref")
    let lines, status = host.RunToLines(cmd)
    let tree = new RefTree()
    for line in lines do
      let ok, node = tree.TryParseAndInsert(line)
      //if ok then
      //  cp $"\fy%s{node.Value.Value.ToString()} \fg%s{node.Parent.FullName}\f0/\fc{node.ShortName}"
      //else
      //  cp $"\fr%s{line}"
      ()
    let job = tree.ToJson()
    let json = JsonConvert.SerializeObject(job, Formatting.Indented);
    cp $"{json}"
    status

