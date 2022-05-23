// (c) 2022  VTT / TTELCL
module Usage

open CommonTools
open ColorPrint

let usage detailed =
  cp "\fohypergit list\f0 [\fg-f <cfgfile>\f0] [\fg-all\f0|\fg-m <filter>\f0]"
  cp "   List enabled repositories in the config file"
  cp "\fohypergit clone\f0 [\fg-f <cfgfile>\f0] [\fg-x\f0] [\fg-all\f0|\fg-m <filter>\f0]"
  cp "   Print commands for cloning missing repositories as bare mirrors."
  cp "   Also creates missing parent folders"
  cp "   \fg-x\f0  Execute the generated GIT commands as child processes"
  cp "\fohypergit update\f0 [\fg-f <cfgfile>\f0] [\fg-x\f0 [\fg-V\f0]] [\fg-all\f0|\fg-m <filter>\f0]"
  cp "   Print the commands for updating existing local mirror repos from"
  cp "   the remotes."
  cp "   \fg-x\f0         Execute the generated GIT commands as child processes"
  cp "   \fg-V\f0         Run GIT verbosely (without making hypergit itself verbose)"
  cp "\fg-v\f0            Verbose mode"
  cp "\fg-f <cfgfile>\f0  Use the specified config file."
  cp "\fx\fx              Default is '\fcrepos.csv\f0' in current directory"
  cp "\fg-all\f0          Apply command to all enabled repositories for this host"
  cp "\fg-m <filter>\f0   Only apply to repositories matching the filter string"
  cp "\fx\fx              A repo matches if \fg<filter>\f0 occurs in \fy<bucket>\fo/\fy<name>\f0"
  


