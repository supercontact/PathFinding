using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Vertex {
	/// <summary>
	/// The position of the vertex.
	/// </summary>
	public Vector3 p;
	/// <summary>
	/// One of the halfedge pointing to the vertex.
	/// </summary>
	public Halfedge edge;
	/// <summary>
	/// A temporary list to store all edges.
	/// </summary>
	public List<Halfedge> edges;
	/// <summary>
	/// A temporary variable to store an index of the vertex.
	/// </summary>
	public int index = 0;
	public bool onBorder;

	public Vertex(Vector3 pos) {
		p = pos;
		onBorder = true;
	}

	public double CalculateVertexAreaTri() {
		double result = 0;
		FillEdgeArray();
		foreach (Halfedge edge in edges) {
			result += edge.face.CalculateAreaTri();
		}
		result /= 3;
		ClearEdgeArray();
		return result;
	}
	public void FillEdgeArray() {
		edges = new List<Halfedge>();
		Halfedge first = edge, temp = edge;
		do {
			edges.Add(temp);
			temp = temp.next.opposite;
		} while (temp != null && temp != first);
		if (temp == null) {
			temp = first;
			while (temp.opposite != null) {
				temp = temp.opposite.prev;
				edges.Add(temp);
			}
		}
	}
	public void ClearEdgeArray() {
		edges = null;
	}
}

public class Face {
	/// <summary>
	/// One of the halfedge which belongs to the face
	/// </summary>
	public Halfedge edge;
	/// <summary>
	/// A temporary list to store all edges.
	/// </summary>
	public List<Halfedge> edges;
	public int index;

	public double CalculateAreaTri() {
		Vector3 a = edge.vertex.p;
		Vector3 b = edge.next.vertex.p;
		Vector3 c = edge.prev.vertex.p;
		return Vector3.Cross((b-a), (c-a)).magnitude / 2;
	}
	public Vector3 CalculateNormalTri() {
		Vector3 a = edge.vertex.p;
		Vector3 b = edge.next.vertex.p;
		Vector3 c = edge.prev.vertex.p;
		return Vector3.Cross((b-a), (c-a)).normalized;
	}
	public Vector3 CalculateCenter() {
		FillEdgeArray();
		Vector3 c = new Vector3();
		foreach(Halfedge edge in edges) {
			c += edge.vertex.p;
		}
		c /= edges.Count;
		ClearEdgeArray();
		return c;
	}
	public void FillEdgeArray() {
		edges = new List<Halfedge>();
		Halfedge first = edge, temp = edge;
		do {
			edges.Add(temp);
			temp = temp.next;
		} while (temp != first);
	}
	public void ClearEdgeArray() {
		edges = null;
	}
}

public class Halfedge {
	public Halfedge next;
	public Halfedge prev;
	public Halfedge opposite;
	/// <summary>
	/// The vertex it points to;
	/// </summary>
	public Vertex vertex;
	public Face face;

	public double Length() {
		return (vertex.p - prev.vertex.p).magnitude;
	}
}

public class Geometry {
	public Mesh linkedMesh;
	public List<Vertex> vertices;
	public List<Halfedge> halfedges;
	public List<Face> faces;
	public List<Face> boundaries;
	public double h;
	public Geometry() {
		vertices = new List<Vertex>();
		halfedges = new List<Halfedge>();
		faces = new List<Face>();
		boundaries = new List<Face>();
	}
	public Geometry(Mesh mesh) {
		vertices = new List<Vertex>();
		halfedges = new List<Halfedge>();
		faces = new List<Face>();
		boundaries = new List<Face>();
		FromMesh(mesh);
	}

	~Geometry() {
		Clear();
	}

	public void Clear() {
		foreach (Vertex v in vertices) {
			v.edge = null;
			v.edges = null;
			v.ClearEdgeArray();
		}
		foreach (Halfedge e in halfedges) {
			e.next = null;
			e.prev = null;
			e.opposite = null;
			e.face = null;
			e.vertex = null;
		}
		foreach (Face f in faces) {
			f.edge = null;
			f.ClearEdgeArray();
		}
		vertices.Clear();
		halfedges.Clear();
		faces.Clear();
	}

	public void FromMesh(Mesh mesh) {
		linkedMesh = mesh;
		Clear();
		Vector3[] meshVerts = mesh.vertices;
		for (int i = 0; i < meshVerts.Length; i++) {
			vertices.Add(new Vertex(meshVerts[i]));
			vertices[i].index = i;
			vertices[i].edges = new List<Halfedge>();
		}
		
		int[] meshFaces = mesh.triangles;
		for (int i = 0; i < meshFaces.Length / 3; i++) {
			Face trig = new Face();
			Halfedge e1 = new Halfedge(), e2 = new Halfedge(), e3 = new Halfedge();
			e1.face = trig;
			e1.next = e2;
			e1.prev = e3;
			e1.vertex = vertices[meshFaces[3*i]];
			e2.face = trig;
			e2.next = e3;
			e2.prev = e1;
			e2.vertex = vertices[meshFaces[3*i+1]];
			e3.face = trig;
			e3.next = e1;
			e3.prev = e2;
			e3.vertex = vertices[meshFaces[3*i+2]];
			trig.edge = e1;
			trig.index = i;
			
			faces.Add(trig);
			halfedges.Add(e1);
			halfedges.Add(e2);
			halfedges.Add(e3);
			e1.vertex.edge = e1;
			e1.vertex.edges.Add(e1);
			e2.vertex.edge = e2;
			e2.vertex.edges.Add(e2);
			e3.vertex.edge = e3;
			e3.vertex.edges.Add(e3);
		}
		
		for (int i = 0; i < vertices.Count; i++) {
			for (int j = 0; j < vertices[i].edges.Count; j++) {
				vertices[i].onBorder = false;
				Halfedge et = vertices[i].edges[j];
				if (et.opposite == null) {
					Vertex vt = et.prev.vertex;
					for (int k = 0; k < vt.edges.Count; k++) {
						if (vt.edges[k].prev.vertex == vertices[i]) {
							et.opposite = vt.edges[k];
							vt.edges[k].opposite = et;
							break;
						}
					}
					if (et.opposite == null) {
						vertices[i].onBorder = true;
						et.opposite = new Halfedge();
						et.opposite.opposite = et;
						et.opposite.vertex = et.prev.vertex;
						halfedges.Add(et.opposite);
						//et.prev.vertex.edges.Add(et.opposite);
					}
				}
			}
		}
		for (int i = 0; i < halfedges.Count; i++) {
			if (halfedges[i].next == null) {
				// Connect all halfedges of this boundary
				Face boundary = new Face();
				boundaries.Add(boundary);
				boundary.edge = halfedges[i];
				boundary.index = -1;
				Halfedge first = halfedges[i], temp = halfedges[i];
				do {
					Halfedge next = temp.opposite;
					while (next.prev != null) {
						next = next.prev.opposite;
					}
					temp.next = next;
					next.prev = temp;
					temp.face = boundary;
					temp.vertex.edges.Add(temp);
					temp = next;
				} while (temp != first);
			}
		}

		/*for (int i = 0; i < boundaries.Count; i++) {
			boundaries[i].FillEdgeArray();
			Debug.Log("C: "+boundaries[i].edges.Count);
		}*/

		for (int i = 0; i < vertices.Count; i++) {
			vertices[i].ClearEdgeArray();
		}

		h = 0;
		foreach (Halfedge e in halfedges) {
			h += e.Length();
		}
		h /= halfedges.Count;
	}

	public void ToMesh(Mesh mesh) {
		Vector3[] verts = new Vector3[vertices.Count];
		for (int i = 0; i < vertices.Count; i++) {
			verts[i] = vertices[i].p;
			vertices[i].index = i;
		}
		int[] trigs = new int[faces.Count * 3];
		for (int i = 0; i < faces.Count; i++) {
			trigs[3*i] = faces[i].edge.vertex.index;
			trigs[3*i+1] = faces[i].edge.next.vertex.index;
			trigs[3*i+2] = faces[i].edge.prev.vertex.index;
		}
		mesh.vertices = verts;
		mesh.triangles = trigs;
	}
	
	/*public void MergeOverlappingVertices() {
		List<Vertex> borderVertices = new List<Vertex>();
		for (int i = 0; i < vertices.Count; i++) {
			if (vertices[i].onBorder) {
				borderVertices.Add(vertices[i]);
			}
		}
		for (int i = 0; i < vertices.Count; i++) {

		}
	}

	public void Merge2Vertices(Vertex v1, Vertex v2) {

	}*/

	public double[,] CalculateLcMatrix(double factor = 1) {
		int n = vertices.Count;
		double[,] result = new double[n,n];
		for (int i = 0; i < n; i++) {
			vertices[i].FillEdgeArray();
			Vector3 vi = vertices[i].p;
			foreach (Halfedge e in vertices[i].edges) {
				int j = e.prev.vertex.index;
				Vector3 vj = e.prev.vertex.p;
				Vector3 va = e.next.vertex.p;
				Vector3 vb = e.opposite.next.vertex.p;
				double cosa = Vector3.Dot((vi - va), (vj - va)) / (vi - va).magnitude / (vj - va).magnitude;
				double cota = cosa / Math.Sqrt(1 - cosa * cosa);
				double cosb = Vector3.Dot((vi - vb), (vj - vb)) / (vi - vb).magnitude / (vj - vb).magnitude;
				double cotb = cosb / Math.Sqrt(1 - cosb * cosb);
				result[i,i] -= factor * (cota + cotb) / 2;
				result[i,j] += factor * (cota + cotb) / 2;
			}
		}
		return result;
	}
}

