using UnityEngine;
using System.Collections;

public class HeatGeodesics {

	public Geometry g;
	public double[,] A1LU, A2LU;
	public int[] A1p, A2p;

	public HeatGeodesics(Geometry g) {
		this.g = g;
	}

	public void Initialize() {
		int n = g.vertices.Count;
		A1LU = g.CalculateLcMatrix(- g.h * g.h);
		for (int i = 0; i < n; i++) {
			A1LU[i,i] += g.vertices[i].CalculateVertexAreaTri();
		}
		alglib.rmatrixlu(ref A1LU, n, n, out A1p);
	}

	public void CalculateGeodesics(Vertex source) {
		int n = g.vertices.Count;
		int f = g.faces.Count;
		double[] b = new double[n];
		b[source.index] = 1;
		int info;
		alglib.densesolverreport rep;
		double[] u;
		alglib.rmatrixlusolve(A1LU, A1p, n, b, out info, out rep, out u);

		//Visualize heat field
		Color[] colors = new Color[n];
		for (int i = 0; i < n; i++) {
			colors[i] = Color.Lerp(Color.blue, Color.white, (float) (0.05 * System.Math.Log(u[i])+1));
		}
		g.linkedMesh.colors = colors;

		Vector3[] X = new Vector3[f];
		for (int i = 0; i < f; i++) {
			Face face = g.faces[i];
			Vector3 normal = face.CalculateNormalTri();
			face.FillEdgeArray();
			double gx = 0, gy = 0, gz = 0;
			foreach (Halfedge edge in face.edges) {
				Vector3 temp = Vector3.Cross(normal, edge.prev.vertex.p - edge.next.vertex.p);
				gx += u[edge.vertex.index] * temp.x;
				gy += u[edge.vertex.index] * temp.y;
				gz += u[edge.vertex.index] * temp.z;
			}
			double l = System.Math.Sqrt(gx * gx + gy * gy + gz * gz);
			X[i] = new Vector3((float) (gx / l), (float) (gy / l), (float) (gz / l));
			face.ClearEdgeArray();

			//Visualize flow
			GameObject stick = GameObject.Instantiate(GameObject.Find("Edge"));
			stick.transform.position = face.CalculateCenter();
			stick.transform.localScale = new Vector3(0.01f, 0.01f, 0.05f);
			stick.transform.rotation = Quaternion.LookRotation(X[i]);
		}



	}

}
