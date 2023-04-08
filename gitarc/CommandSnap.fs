module CommandSnap

open System
open System.IO

open Newtonsoft.Json
open Newtonsoft.Json.Linq

open LcGitLib.GitModels
open LcGitLib.GitModels.Refs

open ColorPrint
open CommonTools

type Dictionary<'k,'v> = System.Collections.Generic.Dictionary<'k,'v>

type private Options = {
  RepoFolder: string option
  DeleteArchiveBranches: bool
  DebugTag: string
  SoftVerbose: bool
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

let private mergeNodes nodes =
  let mergePair node1 node2 =
    if node1.Key <> node2.Key then
      failwith "Internal error, expecting nodes with same key"
    if node1.BranchName <> node2.BranchName then
      failwith "Internal error, expecting nodes with same branch names"
    match node1.PlainNode, node1.ArchiveNode, node2.PlainNode, node2.ArchiveNode with
    | Some(p), None, None, Some(a) ->
      {
        Key = node1.Key
        BranchName = node1.BranchName
        PlainNode = Some(p)
        ArchiveNode = Some(a)
        CanDelete = node1.CanDelete || node2.CanDelete
      }
    | None, Some(a), Some(p), None ->
      {
        Key = node1.Key
        BranchName = node1.BranchName
        PlainNode = Some(p)
        ArchiveNode = Some(a)
        CanDelete = node1.CanDelete || node2.CanDelete
      }
    | _ -> failwith $"Unexpected combination of nodes at key '{node1.Key}'"
  nodes |> Seq.reduce mergePair

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-i" :: rest ->
      rest |> parseMore {o with SoftVerbose = true}
    | "-C" :: folder :: rest ->
      rest |> parseMore {o with RepoFolder = Some(folder)}
    | "-d" :: rest ->
      rest |> parseMore {o with DeleteArchiveBranches = true}
    | "-p" :: rest ->
      rest |> parseMore {o with DeleteArchiveBranches = false}
    | "-tag" :: debugTag :: rest ->
      rest |> parseMore {o with DebugTag = debugTag}
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
      DebugTag = null
      SoftVerbose = false
    }
  match oo with
  | None ->
    1
  | Some(o) ->
    let updateRef target source =
      let cmd =
        GitExecute.gitCommand "update-ref" o.RepoFolder
        |> GitExecute.cmdAddPost1 target
        |> GitExecute.cmdAddPost1 source
      cmd.RunToConsole()
    let delBranch branch =
      cp $"\frNYI \fodelete-branch \fy%s{branch}"
      1
    let cmd = GitExecute.gitShowRefCommand o.RepoFolder
    let lines, status = cmd.RunToLines()
    if status <> 0 then
      cp $"\frError \foStatus %d{status}"
      status
    else
      let tree = lines |> buildTree
      let refNodes =
        tree.EnumerateAllValuedNodes()
        |> Seq.choose classifyNode
        |> Seq.toArray
      
      if o.DebugTag |> String.IsNullOrEmpty |> not then
        let rnJsonArray = refNodes |> refDataJson
        let j1name = $"{o.DebugTag}.pre-merge.json"
        do
          use w = j1name |> startFile
          let json = JsonConvert.SerializeObject(rnJsonArray, Formatting.Indented)
          w.WriteLine(json)
        j1name |> finishFile

      //
      let grouped =
        refNodes
        |> Seq.groupBy (fun rn -> rn.Key)

      let merged =
        grouped
        |> Seq.map (fun (_, values) -> values |> mergeNodes)
        |> Seq.toArray
      
      if o.DebugTag |> String.IsNullOrEmpty |> not then
        let rnJsonArray = merged |> refDataJson
        let j1name = $"{o.DebugTag}.post-merge.json"
        do
          use w = j1name |> startFile
          let json = JsonConvert.SerializeObject(rnJsonArray, Formatting.Indented)
          w.WriteLine(json)
        j1name |> finishFile

      for node in merged do
        match node.PlainNode, node.ArchiveNode, node.CanDelete with
        | Some(p), Some(a), canDelete ->
          let pv = p.Value.Value 
          let av = a.Value.Value
          let ok =
            if pv <> av then
              // update needed
              cp $"\fc%s{a.FullName}\f0 <- \fy%s{p.FullName}\f0"
              false
            else
              if verbose || o.SoftVerbose then
                cp $"\fC%s{a.FullName}\f0 is up to date"
              true
          if canDelete && o.DeleteArchiveBranches then
            if not ok then
              cp $"\foBranch deletion suppressed"
            else
              cp $"\foDeleting branch \f0'\fr%s{node.BranchName.Value}\f0'"
              cp "\frNYI"
          ()
        | Some(p), None, canDelete ->
          let archiveNodeName = "refs/archive/" + node.Key
          cp $"\fg%s{archiveNodeName}\f0 (new) <- \fy%s{p.FullName}\f0"
          ()
        | None, Some(a), _ ->
          if verbose || o.SoftVerbose then
            cp $"\fB%s{a.FullName}\f0 is frozen"
          ()
        | None, None, _ ->
          failwith $"Unexpected node state: no active nor archive node for key '{node.Key}'"
        ()

      0



