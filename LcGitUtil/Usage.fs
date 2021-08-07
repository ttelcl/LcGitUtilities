// (c) 2021  VTT / TTELCL
module Usage

open CommonTools
open PrintUtils

let usage() =
  py "General description"
  pn ""
  py "lcgitutil <command> <arguments>"
  pn "  Wrapper around git.exe"
  py "Common options:"
  p2 "  -v               Verbose mode"
  p2 "  -C <folder>      Make GIT act as if invoked from <folder>"
  p2 "                   Some commands also understand a '<label>::<role>' identifier"
  py "lcgitutil log [-v] [-n <limit>] [-raw] [-save <filename>|-fmt <format>]"
  pn "  Wraps 'git log'"
  p2 "  -n <limit>       Process at most <limit> entries"
  p2 "  -raw             Emit all branches, in raw format. GIT options: --pretty=raw --all"
  p2 "  -save <file>     Save the output to a file. The format depends on the extension."
  p2 "                   Extensions other than *.txt require -raw as well."
  p2 "                   Supported formats: *.txt, *.csv, *.mjson, *.jsonl, *.json,"
  p2 "                   *.summary.mjson, *.summary.jsonl, *.summary.json"
  p2 "  -fmt <format>    Prints interpretations of the raw log records to the console in"
  p2 "                   the given file format: one of 'mjson', 'json', 'jsonl'."
  p2 "                   Implies -raw. Exclusive with -save."
  p2 "  -json            Shortcut for '-fmt mjson'"
  p2 "  + <args>         Pass all remaining arguments to GIT"
  py "lcgitutil rawlog ..."
  pn "  Shortcut for lcgitutil log -raw ..."
  py "lcgitutil newest ..."
  pn "  Shortcut for lcgitutil log -n 1 -json"
  py "lcgitutil findrepo [-C <folder>]"
  pn "  Try to find the GIT repository for the folder"
  py "lcgitutil recipe <recipe> [-C <folder>] [-v]"
  pn "  Run a predefined recipe. Invoke without a recipe name for help."
  py "lcgitutil cfg [-init|-check]"
  pn "  Ensure the lcgitutil configuration file exists, create it if it doesn't yet"
  py "lcgitutil repocfg [-C <folder>] [-dump|-summary]"
  pn "  Interpret the git configuration for the repo"
  p2 "  -dump            Dump as JSON to console"
  p2 "  -summary         Summarize to console"
  p2 "  -compact         Use compact JSON"
  p2 "  -extracompact    Use more compact JSON (structure may not be reliable)"
  py "lcgitutil db ..."
  pn "  Manage the meta-repository database. Invoke without further arguments for help."
  py "lcgitutil graph ..."
  pn "  Various commit graph based tools. Invoke without further arguments for help."
  


