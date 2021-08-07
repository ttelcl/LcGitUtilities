/*
 * (c) 2021  VTT / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib.GraphModel
{
  /// <summary>
  /// Extension methods related to the classes in this namespace
  /// </summary>
  public static class GraphExtensions
  {
    /// <summary>
    /// Capture the node IDs into a new GraphNodeSet for the given graph
    /// </summary>
    /// <param name="ids">
    /// The ids to collect
    /// </param>
    /// <param name="graph">
    /// The graph for which to create a node set
    /// </param>
    /// <param name="nullWhenMissing">
    /// Set the behaviour when asking the resulting set for nodes it does not have.
    /// </param>
    /// <param name="skipWhenMissing">
    /// The behaviour when mapping the input IDs to nodes in the graph
    /// </param>
    public static GraphNodeSet<TId, TSeed> ToSet<TId, TSeed>(
      this IEnumerable<TId> ids,
      Graph<TId, TSeed> graph,
      bool nullWhenMissing = false,
      bool skipWhenMissing = false)
      where TSeed : IGraphNodeSeed<TId>
      where TId : IEquatable<TId>
    {
      return ids.AddToSet(graph.NewSet(nullWhenMissing), skipWhenMissing);
    }

    /// <summary>
    /// Add nodes to an existing node set
    /// </summary>
    /// <param name="ids">
    /// The IDs of the nodes to add
    /// </param>
    /// <param name="nodeSet">
    /// The nodeset to add nodes to
    /// </param>
    /// <param name="skipWhenMissing">
    /// The behaviour when mapping the input IDs to nodes in the graph
    /// </param>
    /// <returns>
    /// The nodeset
    /// </returns>
    public static GraphNodeSet<TId, TSeed> AddToSet<TId, TSeed>(
      this IEnumerable<TId> ids,
      GraphNodeSet<TId, TSeed> nodeSet,
      bool skipWhenMissing = false)
      where TSeed : IGraphNodeSeed<TId>
      where TId : IEquatable<TId>
    {
      nodeSet.Insert(ids, skipWhenMissing);
      return nodeSet;
    }

    /// <summary>
    /// Enumerate all parent nodes of nodes in the set.
    /// The result quite likely will contain duplicates.
    /// </summary>
    public static IEnumerable<Graph<TId, TSeed>.Node> EnumParents<TId, TSeed>(
      this GraphNodeSet<TId, TSeed> nodes)
      where TSeed : IGraphNodeSeed<TId>
      where TId : IEquatable<TId>
    {
      return nodes.Values.SelectMany(node => node.Parents);
    }

    /// <summary>
    /// Enumerate all child nodes of nodes in the set.
    /// The result quite likely will contain duplicates.
    /// </summary>
    public static IEnumerable<Graph<TId, TSeed>.Node> EnumChildren<TId, TSeed>(
      this GraphNodeSet<TId, TSeed> nodes)
      where TSeed : IGraphNodeSeed<TId>
      where TId : IEquatable<TId>
    {
      return nodes.Values.SelectMany(node => node.Children);
    }

  }
}
