// (c) 2022  VTT / TTELCL
module Usage

open CommonTools
open ColorPrint

let usage detailed =
  cp "\fohypergit list\f0 [\fg-f <cfgfile>\f0]"
  cp "   List enabled repositories in the config file"
  cp "\fohypergit clone\f0 [\fg-f <cfgfile>\f0] [\fg-x\f0]"
  cp "   Print commands for cloning missing repositories"
  cp "   Also creates missing parent folders"
  cp "   \fg-x\f0  Execute the generated GIT commands as child processes"
  cp "\fg-v               \f0Verbose mode"



