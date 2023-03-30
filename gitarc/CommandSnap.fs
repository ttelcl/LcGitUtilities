module CommandSnap

open System
open System.IO

open Newtonsoft.Json
open Newtonsoft.Json.Linq

open LcGitLib.GitModels
open LcGitLib.GitModels.Refs

open ColorPrint
open CommonTools

type private Options = {
  RepoFolder: string option
  DeleteArchiveBranches: bool
}

type private RefData = {
  Key: string
  BranchName: string option
  ArchiveNode: RefTreeNode option
  PlainNode: RefTreeNode option
  CanDelete: bool
}

let rtn2json (node: RefTreeNode) =
  let job = new JObject()
  job["path"] <- node.FullName
  if node.Value.HasValue then
    job["target"] <- node.Value.Value.Id
  job

let private rd2json rd =
  let job = new JObject()
  job["key"] <- rd.Key
  match rd.BranchName with
  | Some(bn) -> job["branch"] <- bn
  | None -> ()
  match rd.ArchiveNode with
  | Some(an) -> job["archive"] <- an |> rtn2json
  | None -> ()
  match rd.PlainNode with
  | Some(pn) -> job["node"] <- pn |> rtn2json
  | None -> ()
  if rd.CanDelete then
    job["canDelete"] <- rd.CanDelete
  job

let private refDataJson refdatas =
  let ja = new JArray()
  for rd in refdatas do
    rd |> rd2json |> ja.Add
  ja

let private buildTree lines =
  let tree = new RefTree()
  for line in lines do
    let ok, _ = tree.TryParseAndInsert(line)
    if ok |> not then
      ecp $"\frSKIP:\fo {line}"
  tree

let hasPrefix prefix (node: RefTreeNode) =
  let ok, tail = node.HasPrefix(prefix)
  if ok then Some(tail) else None

let (|Tag|_|) node = node |> hasPrefix "refs/tags/"
let (|BranchArchive|_|) node = node |> hasPrefix "refs/archive/heads/"
let (|Archive|_|) node = node |> hasPrefix "refs/archive/"
let (|ArchiveBranch|_|) node = node |> hasPrefix "refs/heads/archive/"
let (|Branch|_|) node = node |> hasPrefix "refs/heads/"
let (|Other|_|) node = node |> hasPrefix "refs/"

let private classifyNode (node: RefTreeNode) =
  match node with
  | Tag _ -> // tags. Ignored
    None
  | BranchArchive key ->  // an archive node of a branch
    Some({
      Key = "heads/" + key
      BranchName = Some(key)
      ArchiveNode = Some(node)
      PlainNode = None
      CanDelete = false
    })
  | Archive key -> // a non-branch archive node
    Some({
      Key = key
      BranchName = None
      ArchiveNode = Some(node)
      PlainNode = None
      CanDelete = false
    })
  | ArchiveBranch key -> // a branch renamed to be archivable
    Some({
      Key = "heads/archive/" + key
      BranchName = Some("archive/" + key)
      ArchiveNode = None
      PlainNode = Some(node)
      CanDelete = true
    })
  | Branch key -> // an ordinary branch
    Some({
      Key = "heads/" + key
      BranchName = Some(key)
      ArchiveNode = None
      PlainNode = Some(node)
      CanDelete = false
    })
  | Other key -> // some ref that is not a tag, branch or archive node. Maybe a remote tracking branch?
    Some({
      Key = key
      BranchName = None
      ArchiveNode = None
      PlainNode = Some(node)
      CanDelete = false
    })
  | _ ->
    cp $"\frUnrecognized: \fo{node.FullName}"
    None

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-C" :: folder :: rest ->
      rest |> parseMore {o with RepoFolder = Some(folder)}
    | "-d" :: rest ->
      rest |> parseMore {o with DeleteArchiveBranches = true}
    | "-p" :: rest ->
      rest |> parseMore {o with DeleteArchiveBranches = false}
    | "-h" :: _
    | "-help" :: _ ->
      Usage.usage "snap"
      None
    | [] ->
      Some(o)
    | x :: _ ->
      failwith $"Unrecognized argument: %s{x}"
  let oo = 
    args |> parseMore {
      RepoFolder = None
      DeleteArchiveBranches = false
    }
  match oo with
  | None ->
    1
  | Some(o) ->
    let cmd = GitExecute.gitShowRefCommand o.RepoFolder
    let lines, status = cmd.RunToLines()
    if status <> 0 then
      cp $"\frError\foStatus %d{status}"
      status
    else
      let tree = lines |> buildTree
      let refNodes =
        tree.EnumerateAllValuedNodes()
        |> Seq.choose classifyNode
        |> Seq.toArray
      
      // Debug
      let rnJsonArray = refNodes |> refDataJson
      let j1name = "dump1.json"
      do
        use w = j1name |> startFile
        let json = JsonConvert.SerializeObject(rnJsonArray, Formatting.Indented)
        w.WriteLine(json)
      j1name |> finishFile

      0



