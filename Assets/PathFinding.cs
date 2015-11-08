using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using C5; 

public class Node {
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

public class NodeDistanceComparer : IComparer<Node> {
	public int Compare (Node x, Node y)
	{
		return y.distance - x.distance < 0 ? 1 : (y.distance - x.distance > 0 ? -1 : 0);
	}
}

public class PathFinding {

	public List<Node> nodes;

	public void ShortestPathSimple(Geometry g, Vertex source) {
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
			UnityEngine.Debug.Log(node.distance);
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

	public void DrawPathFrom(Vertex v) {
		Node node = nodes[v.index];
		while (node.distance != 0) {
			DrawEdge (node, node.prev);
			node = node.prev;
		}
	}

	public void DrawEdge(Node n1, Node n2) {
		GameObject stick = GameObject.Instantiate(GameObject.Find("Edge"));
		stick.transform.position = (n1.v.p + n2.v.p) / 2;
		float scale = (n1.v.p - n2.v.p).magnitude / 2f;
		stick.transform.localScale = new Vector3(0.02f, 0.02f, scale);
		stick.transform.rotation = Quaternion.LookRotation(n1.v.p - n2.v.p);
	}

	public void DrawAllBorder() {
		foreach (Node n in nodes) {
			if (n.v.onBorder) {
				GameObject stick = GameObject.Instantiate(GameObject.Find("Edge"));
				stick.transform.position = n.v.p;
				stick.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
			}
		}
	}
}
