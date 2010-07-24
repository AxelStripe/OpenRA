#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Traits
{
	class RoadmapInfo : TraitInfo<Roadmap> { }

	class Roadmap : IGameStarted
	{
		List<Node> nodes = new List<Node>();

		public void GameStarted(World w)
		{
			/* generate a bunch of random positions, and insert them into a graph */
			/* generate edges according to 'reachability' by a naive planner */
		}

		public List<int2> GetPath(int2 from, int2 to)
		{
			/* temporarily insert `from` and `to` into the graph. 
			 * then run A* over the graph (according to euclidean distance?)
			 * splice together the paths, and you win */

			return null;
		}

		public bool IsReachable(int2 from, int2 to)
		{
			/* as in GetPath, but no actual path construction is required -- only
			 * a reachability check, which is somewhat cheaper! */

			return false;
		}

		class Node
		{
			public int2 Location;
			public Dictionary<Node, Edge> Edges = new Dictionary<Node, Edge>();
		}

		class Edge
		{
			public Node from;
			public Node to;
			public float cost;

			public static Edge Reverse(Edge e)
			{
				return new Edge { from = e.to, to = e.from, cost = e.cost };
			}
		}
	}
}
