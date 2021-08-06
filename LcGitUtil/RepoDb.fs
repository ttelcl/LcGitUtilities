module RepoDb

open System
open System.IO

open LcGitLib
open LcGitLib.GitRunning
open LcGitLib.FileUtilities
open LcGitLib.RawLog
open LcGitLib.RepoTools
open LcGitLib.MetaBase

open CommonTools
open PrintUtils
open ArgumentLogger

let resolveRepoWitness (witness: string) =
  if witness.Contains("::") then
    let repos = lazyRepos.Force()
    let matches = repos.LabelRoleMatches(witness) |> Seq.toArray
    let m =
      match matches.Length with
      | 0 ->
        failwithf "'%s' did not match any repos" witness
      | 1 ->
        matches.[0]
      | n ->
        // ambiguous ... unless there is precisely one exact match
        let exactmatches =
          matches
          |> Seq.where (fun rib -> rib.MatchesLabelAndRole(witness, true))
          |> Seq.toArray
        if exactmatches.Length = 1 then
          exactmatches.[0]
        else
          failwithf "'%s' matched %d repositories (%d exact) instead of 1" witness n exactmatches.Length
    m.Location
  else
    if witness |> String.IsNullOrEmpty then
      Environment.CurrentDirectory
    else
      Path.GetFullPath(witness)


