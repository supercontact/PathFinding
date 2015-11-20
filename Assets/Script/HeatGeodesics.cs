using UnityEngine;
using System.Collections;

/// <summary>
/// The class which performs heat method on a geometry to calculate distance field.
/// </summary>
public class HeatGeodesics {

	/// <summary>
	/// The geometry (mesh) the heat method is running on.
	/// </summary>
	public Geometry g;
	/// <summary>
	/// The source vertex.
	/// </summary>
	public Vertex s;

	// Precalculated matrices and data
	public alglib.sparsematrix A1, A1b, A2;
	public Vector3[] div;

	/// <summary>
	/// The heat field, stored on vertices.
	/// </summary>
	public double[] u;
	/// <summary>
	/// Normalized gradient of heat field, stored on triangles.
	/// </summary>
	public Vector3[] X;
	/// <summary>
	/// Div(X), stored on vertices.
	/// </summary>
	public double[] divX;
	/// <summary>
	/// The distance field, stored on vertices.
	/// </summary>
	public double[] phi;
	/// <summary>
	/// Gradient of distance field, stored on triangles.
	/// </summary>
	public Vector3[] GradPhi;

	public HeatGeodesics(Geometry g) {
		this.g = g;
	}

	/// <summary>
	/// Call this to build matrices.
	/// </summary>
	public void Initialize() {
		int n = g.vertices.Count;

		// Heat matrix (Neumann condition)
		A1 = g.CalculateLcMatrixSparse(- Settings.tFactor * g.h * g.h);
		for (int i = 0; i < n; i++) {
			alglib.sparseadd(A1, i, i, g.vertices[i].CalculateVertexAreaTri());
		}
		alglib.sparseconverttocrs(A1);

		if (g.hasBorder) {
			// Dirichlet condition heat matrix
			A1b = g.CalculateLcMatrixSparse(- Settings.tFactor * g.h * g.h, true);
			for (int i = 0; i < n; i++) {
				alglib.sparseadd(A1b, i, i, g.vertices[i].CalculateVertexAreaTri());
			}
			alglib.sparseconverttocrs(A1b);
		}

		// Laplacien matrix
		A2 = g.CalculateLcMatrixSparse(-1);
		alglib.sparseconverttocrs(A2);

		div = g.CalculateDivData();
	}

	/// <summary>
	/// Start the calculation.
	/// </summary>
	public void CalculateGeodesics(Vertex source) {
		s = source;
		int n = g.vertices.Count;
		int f = g.faces.Count;

		float time = Time.realtimeSinceStartup;
		Debug.Log("t = 0ms");

		// Solve heat equation
		double[] b = new double[n];
		b[source.index] = 1;

		alglib.lincgstate s1;
		alglib.lincgreport rep1;
		alglib.lincgcreate(n, out s1);
		alglib.lincgsolvesparse(s1, A1, true, b);
		alglib.lincgresults(s1, out u, out rep1);

		if (g.hasBorder) {
			// Average of Neumann condition solution and Dirichlet condition solution
			double[] u2;
			alglib.lincgstate s1b;
			alglib.lincgreport rep1b;
			alglib.lincgcreate(n, out s1b);
			alglib.lincgsolvesparse(s1b, A1b, true, b);
			alglib.lincgresults(s1b, out u2, out rep1b);
			for (int i = 0; i < u.Length; i++) {
				u[i] = (u[i] + u2[i]) / 2;
			}
		}

		Debug.Log("Solved first linear system, termination = " + rep1.terminationtype);
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");


		// Calculate X, normalized gradient of heat field
		X = new Vector3[f];
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
			if (l == 0) {
				X[i] = new Vector3();
			} else {
				X[i] = new Vector3((float) (gx / l), (float) (gy / l), (float) (gz / l));
			}
			face.ClearEdgeArray();
		}

		Debug.Log("Gradient of heat flow calculated");
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");


		// Calculate div(X);
		divX = new double[n];
		double divXmean = 0;
		for (int i = 0; i < n; i++) {
			g.vertices[i].FillEdgeArray();
			foreach (Halfedge edge in g.vertices[i].edges) {
				if (edge.face.index != -1) {
					int edgeIndex;
					if (edge == edge.face.edge) {
						edgeIndex = 0;
					} else if (edge == edge.face.edge.next) {
						edgeIndex = 1;
					} else {
						edgeIndex = 2;
					}
					divX[i] += Vector3.Dot(div[3*edge.face.index+(edgeIndex+2)%3] - div[3*edge.face.index+(edgeIndex+1)%3], X[edge.face.index]);
				}
			}
			divXmean += divX[i];
		}
		divXmean /= n;
		for (int i = 0; i < n; i++) {
			divX[i] -= divXmean;
		}

		Debug.Log("Div of X calculated");
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");


		// Solve Poisson equation
		alglib.lincgstate s2;
		alglib.lincgreport rep2;
		alglib.lincgcreate(n, out s2);
		alglib.lincgsolvesparse(s2, A2, true, divX);
		alglib.lincgresults(s2, out phi, out rep2);

		double phi0 = phi[source.index];
		for (int i = 0; i < n; i++) {
			phi[i] -= phi0;
		}

		Debug.Log("Distance field calculated (Second linear system), termination = " + rep2.terminationtype);
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

		// Adjust uv coordinates of vertices in order to show the distance mapping
		// Tangent field provides necessary information for normal mapping
		Vector2[] uv = new Vector2[n];
		Vector4[] tangents = new Vector4[n];
		for (int i = 0; i < n; i++) {
			uv[i] = new Vector2((float) (phi[i] / Settings.mappingDistance), 0);
			Vector3 tgt = new Vector3();
			foreach (Halfedge e in g.vertices[i].edges) {
				if (e.face.index != -1) {
					tgt += X[e.face.index];
				}
			}
			tgt.Normalize();
			tangents[i] = new Vector4(tgt.x, tgt.y, tgt.z, 1);
			g.vertices[i].ClearEdgeArray();
		}
		g.linkedMesh.uv = uv;
		g.linkedMesh.tangents = tangents;

		// Calculate gradient of the distance field
		GradPhi = new Vector3[f];
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
			GradPhi[i] = new Vector3((float) (gx / l), (float) (gy / l), (float) (gz / l));
			face.ClearEdgeArray();
		}

	}

}
