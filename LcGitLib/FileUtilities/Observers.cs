/*
 * (c) 2021  VTT / TTELCL
 */

using LcGitLib.RawLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib.FileUtilities
{
  /// <summary>
  /// Static uitilities for instantiating various kinds of observers
  /// </summary>
  public static class Observers
  {
    /// <summary>
    /// Create an observer that writes lines of text to the text writer
    /// and closes it when the observation completes
    /// </summary>
    public static IObserver<string> ObserveToWriter(this TextWriter writer)
    {
      return new LineWritingObserver(writer);
    }

    /// <summary>
    /// Build an observer that writes incoming objects to a JSON-related stream
    /// </summary>
    /// <param name="stringObserver">
    /// The string observer representing the stream to write to
    /// </param>
    /// <param name="mode">
    /// Defines how to wrap the incoming objects to a file
    /// </param>
    public static IObserver<object> ObserveJsonObjects(
      this IObserver<string> stringObserver, JsonMode mode)
    {
      return new JsonWritingObserver(mode, stringObserver);
    }

    /// <summary>
    /// Build an observer that writes incoming objects to a JSON-related stream
    /// </summary>
    /// <param name="stringObserver">
    /// The string observer representing the stream to write to
    /// </param>
    /// <param name="fileTail">
    /// The file name (or just the end of a file name) whose extension determines the format
    /// how to write the entries. Supported formats are *.json, *.jsonl and *.mjson
    /// </param>
    public static IObserver<object> ObserveJsonObjects(
      this IObserver<string> stringObserver, string fileTail)
    {
      var mode = JsonWritingObserver.ModeForExtension(fileTail);
      return new JsonWritingObserver(mode, stringObserver);
    }

    /// <summary>
    /// Build an observer that converts incoming CommitEntries to CSV lines
    /// </summary>
    public static IObserver<CommitEntry> ObserveCsvCommitEntries(
      this IObserver<string> stringObserver)
    {
      return new CommitCsvAdapter(stringObserver);
    }

    /// <summary>
    /// Build an observer that parses GIT raw log lines into CommitEntries and
    /// sends those to the provided sink
    /// </summary>
    public static IObserver<string> ObserveAsCommitEntries(
      this IObserver<CommitEntry> sink)
    {
      return CommitEntry.ObserveFromLines(sink);
    }

    /// <summary>
    /// Build an observer for saving GIT raw log lines to a file
    /// </summary>
    /// <param name="writer">
    /// The already opened file, as a TextWriter
    /// </param>
    /// <param name="fileTail">
    /// The name of the file (or the end of the name of the file, as long as
    /// it includes the file extension). This determines the file format.
    /// Supported extensions are *.txt, *.json, *.jsonl, *.mjson, *.csv
    /// </param>
    public static IObserver<string> RawGitLogObserver(
      this TextWriter writer,
      string fileTail)
    {
      return
        writer
        .ObserveToWriter()
        .RawGitLogObserver(fileTail);
    }

    /// <summary>
    /// Build an observer for saving GIT raw log lines to some kind of
    /// receiver of lines of text
    /// </summary>
    /// <param name="sink">
    /// The destination where to send the (potentially) transformed lines
    /// </param>
    /// <param name="fileTail">
    /// The name of the file (or the end of the name of the file, as long as
    /// it includes the file extension). This determines the file format.
    /// Supported extensions are *.txt, *.json, *.jsonl, *.mjson, *.csv
    /// </param>
    public static IObserver<string> RawGitLogObserver(
      this IObserver<string> sink,
      string fileTail)
    {
      var ext = Path.GetExtension(fileTail).ToLowerInvariant();
      if(ext == ".txt")
      {
        return sink;
      }
      else if(ext == ".csv")
      {
        return
          sink
          .ObserveCsvCommitEntries()
          .ObserveAsCommitEntries();
      }
      else
      {
        return
          sink
          .ObserveJsonObjects(fileTail)
          .ObserveAsCommitEntries();
      }
    }

  }
}
