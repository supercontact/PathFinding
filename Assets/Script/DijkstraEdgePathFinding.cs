using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using C5; 

/// <summary>
/// Vertex with some additional data.
/// </summary>
class Node {
	public Vertex v;
	public Node prev;
	public double distance;
	public IPriorityQueueHandle<Node> handle;

	public enum Status {
		Done,
		Pending,
		Unvisited
	}
	public Status status =  Status.Unvisited;

	public Node(Vertex v) {
		this.v = v;
		this.distance = double.PositiveInfinity;
	}

}

class NodeDistanceComparer : IComparer<Node> {
	public int Compare (Node x, Node y)
	{
		return y.distance - x.distance < 0 ? 1 : (y.distance - x.distance > 0 ? -1 : 0);
	}
}

/// <summary>
/// Finds all shortest on-edge paths to a source point via a simple Dijkstra algorithm.
/// </summary>
public class DijkstraEdgePathFinding {

	private List<Node> nodes;

	public DijkstraEdgePathFinding(Geometry g, Vertex source) {
		nodes = new List<Node> ();
		for (int i = 0; i < g.vertices.Count; i++) {
			nodes.Add (new Node(g.vertices[i]));
			g.vertices[i].index = i;
		}
		IntervalHeap<Node> queue = new IntervalHeap<Node>(new NodeDistanceComparer());
		queue.Add (ref nodes[source.index].handle, nodes[source.index]);
		nodes[source.index].distance = 0;
		while (!queue.IsEmpty) {
			Node node = queue.DeleteMin();
			node.status = Node.Status.Done;
			node.v.FillEdgeArray();
			foreach (Halfedge e in node.v.edges) {
				Node neighbor = nodes[e.prev.vertex.index];
				if (neighbor.status == Node.Status.Pending) {
					if (e.Length() + node.distance < neighbor.distance) {
						neighbor.distance = e.Length() + node.distance;
						neighbor.prev = node;
						queue.Replace(neighbor.handle, neighbor);

					}
				} else if (neighbor.status == Node.Status.Unvisited){
					neighbor.distance = e.Length() + node.distance;
					neighbor.prev = node;
					neighbor.status = Node.Status.Pending;
					queue.Add(ref neighbor.handle, neighbor);
				}
			}
			node.v.ClearEdgeArray();
		}

	}

	/// <summary>
	/// Get all vertices along the shortest on-edge path from v to source.
	/// </summary>
	public List<Vertex> GetPathFrom(Vertex v) {
		Node node = nodes[v.index];
		List<Vertex> result = new List<Vertex>();
		result.Add(v);
		while (node.distance != 0) {
			node = node.prev;
			result.Add(node.v);
		}
		return result;
	}
	
}
