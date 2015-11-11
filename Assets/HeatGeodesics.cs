using UnityEngine;
using System.Collections;

public class HeatGeodesics {

	public Geometry g;
	//public double[,] A1LU, A2LU;
	//public int[] A1p, A2p;
	public alglib.sparsematrix A1, A2;

	public HeatGeodesics(Geometry g) {
		this.g = g;
	}

	public void Initialize() {
		int n = g.vertices.Count;
		/*A1LU = g.CalculateLcMatrix(- g.h * g.h);
		for (int i = 0; i < n; i++) {
			A1LU[i,i] += g.vertices[i].CalculateVertexAreaTri();
		}
		A2LU = g.CalculateLcMatrix();
		alglib.rmatrixlu(ref A1LU, n, n, out A1p);
		alglib.rmatrixlu(ref A2LU, n, n, out A2p);*/
		A1 = g.CalculateLcMatrixSparse(- g.h * g.h);
		for (int i = 0; i < n; i++) {
			alglib.sparseadd(A1, i, i, g.vertices[i].CalculateVertexAreaTri());
		}
		A2 = g.CalculateLcMatrixSparse();
	
		alglib.sparseconverttocrs(A1);
		alglib.sparseconverttocrs(A2);
	}

	public void CalculateGeodesics(Vertex source) {
		float time = Time.realtimeSinceStartup;
		Debug.Log("t = 0ms");

		int n = g.vertices.Count;
		int f = g.faces.Count;
		double[] b = new double[n];
		b[source.index] = 1;
		double[] u;
		/*int info;
		alglib.densesolverreport rep;
		alglib.rmatrixlusolve(A1LU, A1p, n, b, out info, out rep, out u);*/
		alglib.lincgstate s1;
		alglib.lincgreport rep1;
		alglib.lincgcreate(n, out s1);
		alglib.lincgsolvesparse(s1, A1, true, b);
		alglib.lincgresults(s1, out u, out rep1);

		Debug.Log("Solved first linear system, termination = " + rep1.terminationtype);
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

		//Visualize heat field
		/*Color[] colors = new Color[n];
		for (int i = 0; i < n; i++) {
			colors[i] = Color.Lerp(Color.blue, Color.white, (float) (0.05 * System.Math.Log(u[i])+1));
		}
		g.linkedMesh.colors = colors;*/

		GameObject visual = GameObject.Find("FlowVisualization");
		Vector3[] X = new Vector3[f];
		for (int i = 0; i < f; i++) {
			Face face = g.faces[i];
			Vector3 normal = face.CalculateNormalTri();
			face.FillEdgeArray();
			double gx = 0, gy = 0, gz = 0;
			foreach (Halfedge edge in face.edges) {
				Vector3 temp = Vector3.Cross(normal, edge.prev.vertex.p - edge.next.vertex.p);
				gx -= u[edge.vertex.index] * temp.x;
				gy -= u[edge.vertex.index] * temp.y;
				gz -= u[edge.vertex.index] * temp.z;
			}
			double l = System.Math.Sqrt(gx * gx + gy * gy + gz * gz);
			X[i] = new Vector3((float) (gx / l), (float) (gy / l), (float) (gz / l));
			face.ClearEdgeArray();

			//Visualize flow
			/*GameObject stick = GameObject.Instantiate(GameObject.Find("Edge"));
			stick.transform.SetParent(visual.transform);
			stick.transform.position = face.CalculateCenter();
			stick.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
			stick.transform.rotation = Quaternion.LookRotation(X[i], face.CalculateNormalTri());*/
		}

		Debug.Log("Gradient of heat flow calculated");
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

		double[] divX = new double[n];
		for (int i = 0; i < n; i++) {
			g.vertices[i].FillEdgeArray();
			Vector3 v = g.vertices[i].p;
			foreach (Halfedge edge in g.vertices[i].edges) {
				Vector3 v1 = edge.prev.vertex.p;
				Vector3 v2 = edge.next.vertex.p;
				double cos1 = Vector3.Dot(v-v2, v1-v2) / (v-v2).magnitude / (v1-v2).magnitude;
				double cot1 = cos1 / System.Math.Sqrt(1 - cos1 * cos1);
				double cos2 = Vector3.Dot(v-v1, v2-v1) / (v-v1).magnitude / (v2-v1).magnitude;
				double cot2 = cos2 / System.Math.Sqrt(1 - cos2 * cos2);
				divX[i] += cot1 * Vector3.Dot(v1 - v, X[edge.face.index]) + cot2 * Vector3.Dot(v2 - v, X[edge.face.index]);
			}
			divX[i] /= 2;
			g.vertices[i].ClearEdgeArray();
		}

		Debug.Log("Div of X calculated");
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

		double[] phi;
		//alglib.rmatrixlusolve(A2LU, A2p, n, divX, out info, out rep, out phi);
		alglib.linlsqrstate s2;
		alglib.linlsqrreport rep2;
		alglib.linlsqrcreate(n, n, out s2);
		alglib.linlsqrsolvesparse(s2, A2, divX);
		alglib.linlsqrresults(s2, out phi, out rep2);

		double phi0 = phi[source.index];
		double maxDistance = 0;
		for (int i = 0; i < n; i++) {
			phi[i] -= phi0;
			if (phi[i] > maxDistance) {
				maxDistance = phi[i];
			}
		}

		Debug.Log("Distance field calculated (Second linear system), termination = " + rep2.terminationtype);
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

		//Visualize distance field
		/*Color[] colors = new Color[n];
		for (int i = 0; i < n; i++) {
			colors[i] = Color.Lerp(Color.white, Color.blue, (float) (phi[i] / 2));
			if (phi[i] % 0.3 < 0.1) {
				colors[i] = Color.yellow;
			}
		}
		g.linkedMesh.colors = colors;*/

		Vector2[] uv = new Vector2[n];
		for (int i = 0; i < n; i++) {
			uv[i] = new Vector2((float) (phi[i] / maxDistance), 0);
		}
		g.linkedMesh.uv = uv;


		Vector3[] X2 = new Vector3[f];
		for (int i = 0; i < f; i++) {
			Face face = g.faces[i];
			Vector3 normal = face.CalculateNormalTri();
			face.FillEdgeArray();
			double gx = 0, gy = 0, gz = 0;
			foreach (Halfedge edge in face.edges) {
				Vector3 temp = Vector3.Cross(normal, edge.prev.vertex.p - edge.next.vertex.p);
				gx -= phi[edge.vertex.index] * temp.x;
				gy -= phi[edge.vertex.index] * temp.y;
				gz -= phi[edge.vertex.index] * temp.z;
			}
			double l = System.Math.Sqrt(gx * gx + gy * gy + gz * gz);
			X2[i] = new Vector3((float) (gx / l), (float) (gy / l), (float) (gz / l));
			face.ClearEdgeArray();
			
			//Visualize flow2
			/*GameObject stick = GameObject.Instantiate(GameObject.Find("Edge"));
			stick.transform.position = face.CalculateCenter();
			stick.transform.localScale = new Vector3(0.01f, 0.01f, 0.05f);
			stick.transform.rotation = Quaternion.LookRotation(X2[i]);*/
		}
	}

}
