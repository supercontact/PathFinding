using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The class which performs heat method on a geometry to calculate distance field.
/// </summary>
public class HeatGeodesics {

	/// <summary>
	/// The geometry (mesh) the heat method is running on.
	/// </summary>
	public Geometry g;
	/// <summary>
	/// The source vertices.
	/// </summary>
	public List<Vertex> s;
	/// <summary>
	/// Whether or not we use Cholesky decomposition to accelerate the calculation (which will however make initialization slower).
	/// </summary>
	private bool useCholesky;

	// Precalculated matrices and data
	private alglib.sparsematrix A1, A1b, A2;
	private double[] modif1 = null, modif1b = null;
	private bool lastBuiltMatrixIsMultiSource = false;
	private Vector3[] div;

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

	public HeatGeodesics(Geometry g, bool useCholesky = true) {
		this.g = g;
		this.useCholesky = useCholesky;
	}

	/// <summary>
	/// Call this to build matrices.
	/// </summary>
	public void Initialize(bool clearSources = true) {
		float time = Time.realtimeSinceStartup;
		Debug.Log("Initializing matrices...");

		if (clearSources) {
			s = new List<Vertex>();
		}
		bool multiSource = s.Count > 1;
		int n = g.vertices.Count;

		// Heat matrix (Neumann condition)
		A1 = g.CalculateLcMatrixSparse(- Settings.tFactor * g.h * g.h, false, multiSource ? s : null);
		if (multiSource) {
			modif1 = g.modification;
		}
		for (int i = 0; i < n; i++) {
			alglib.sparseadd(A1, i, i, g.vertices[i].CalculateVertexAreaTri());
		}
		if (useCholesky) {
			alglib.sparseconverttosks(A1);
			alglib.sparsecholeskyskyline(A1, n, true);
		} else {
			alglib.sparseconverttocrs(A1);
		}

		Debug.Log("Heat equation matrix built, time = " + (Time.realtimeSinceStartup - time) * 1000 + "ms");
		time = Time.realtimeSinceStartup;


		if (g.hasBorder && Settings.boundaryCondition > 0) {
			// Dirichlet condition heat matrix
			A1b = g.CalculateLcMatrixSparse(- Settings.tFactor * g.h * g.h, true, multiSource ? s : null);
			if (multiSource) {
				modif1b = g.modification;
			}
			for (int i = 0; i < n; i++) {
				alglib.sparseadd(A1b, i, i, g.vertices[i].CalculateVertexAreaTri());
			}
			if (useCholesky) {
				alglib.sparseconverttosks(A1b);
				alglib.sparsecholeskyskyline(A1b, n, true);
			} else {
				alglib.sparseconverttocrs(A1b);
			}

			Debug.Log("Additional heat equation matrix built (dirichlet), time = " + (Time.realtimeSinceStartup - time) * 1000 + "ms");
			time = Time.realtimeSinceStartup;
		}


		// Laplacien matrix
		A2 = g.CalculateLcMatrixSparse(-1, false, multiSource ? s : null);
		if (multiSource) {
			foreach (Vertex src in s) {
				alglib.sparseadd(A2, src.index, src.index, 1);
			}
		}
		if (useCholesky) {
			for (int i = 0; i < n; i++) {
				alglib.sparseadd(A2, i, i, 0.000000001); // To make it positive definite
			}
			alglib.sparseconverttosks(A2);
			alglib.sparsecholeskyskyline(A2, n, true);
		} else {
			alglib.sparseconverttocrs(A2);
		}

		Debug.Log("Laplacien matrix built, time = " + (Time.realtimeSinceStartup - time) * 1000 + "ms");
		time = Time.realtimeSinceStartup;


		// Tables of Cotangent value * edge vectors
		div = g.CalculateDivData();

		Debug.Log("Cotangent table built, time = " + (Time.realtimeSinceStartup - time) * 1000 + "ms");
	}

	/// <summary>
	/// Start calculation with a specific source vertex.
	/// </summary>
	public void CalculateGeodesics(Vertex source, bool additional = false) {
		if (!additional) {
			s.Clear();
		}
		s.Add(source);
		CalculateGeodesics();
	}
	/// <summary>
	/// Start calculation with a collection of source vertices.
	/// </summary>
	public void CalculateGeodesics(IEnumerable<Vertex> sources,  bool additional = false) {
		if (!additional) {
			s.Clear();
		}
		s.AddRange(sources);
		CalculateGeodesics();
	}
	/// <summary>
	/// Start the main calculation.
	/// </summary>
	public void CalculateGeodesics() {
		// If we have multiple sources, we cannot use precalculated matrices. So rebuild matrices with forced heat conditions on sources.
		if (s.Count > 1 || lastBuiltMatrixIsMultiSource) {
			Initialize(false);
			lastBuiltMatrixIsMultiSource = s.Count > 1;
		}


		int n = g.vertices.Count;
		int f = g.faces.Count;

		float time = Time.realtimeSinceStartup;
		Debug.Log("Begin geodesic calculation...");

		// Solve heat equation
		double[] b = new double[n];
		foreach (Vertex source in s) {
			b[source.index] = source.CalculateVertexAreaTri();
		}


		u = new double[n];
		Array.Copy(b, u, n);
		if (s.Count > 1) {
			for (int i = 0; i < n; i++) {
				u[i] = u[i] + modif1[i];
			}
		}

		if (useCholesky) { 
			alglib.sparsetrsv(A1, true, false, 1, ref u);
			alglib.sparsetrsv(A1, true, false, 0, ref u);
		} else {
			alglib.lincgstate s1;
			alglib.lincgreport rep1;
			alglib.lincgcreate(n, out s1);
			alglib.lincgsolvesparse(s1, A1, true, u);
			alglib.lincgresults(s1, out u, out rep1);
		}

		if (g.hasBorder && Settings.boundaryCondition > 0) {
			// The mesh has boundaries : Use average of Neumann condition solution and Dirichlet condition solution
			double[] u2 = new double[n];
			Array.Copy(b, u2, n);
			if (s.Count > 1) {
				for (int i = 0; i < n; i++) {
					u2[i] = u2[i] + modif1b[i];
				}
			}

			if (useCholesky) {
				alglib.sparsetrsv(A1b, true, false, 1, ref u2);
				alglib.sparsetrsv(A1b, true, false, 0, ref u2);
			} else {
				alglib.lincgstate s1b;
				alglib.lincgreport rep1b;
				alglib.lincgcreate(n, out s1b);
				alglib.lincgsolvesparse(s1b, A1b, true, u2);
				alglib.lincgresults(s1b, out u2, out rep1b);
			}
			for (int i = 0; i < u.Length; i++) {
				u[i] = u[i] * (1 - Settings.boundaryCondition) + u2[i] * Settings.boundaryCondition;
			}
		}

		Debug.Log("Solved first linear system, heat field calculated, time = " + (Time.realtimeSinceStartup - time)*1000 + "ms");
		time = Time.realtimeSinceStartup;


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

		Debug.Log("Gradient of heat flow calculated, time = " + (Time.realtimeSinceStartup - time)*1000 + "ms");
		time = Time.realtimeSinceStartup;


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

		Debug.Log("Div of X calculated, time = " + (Time.realtimeSinceStartup - time)*1000 + "ms");
		time = Time.realtimeSinceStartup;


		// Solve Poisson equation
		phi = new double[n];
		Array.Copy(divX, phi, n);
		if (s.Count > 1) {
			foreach (Vertex src in s) {
				phi[src.index] = 0;
			}
		}
		if (useCholesky) {

			alglib.sparsetrsv(A2, true, false, 1, ref phi);
			alglib.sparsetrsv(A2, true, false, 0, ref phi);
		} else {
			alglib.lincgstate s2;
			alglib.lincgreport rep2;
			alglib.lincgcreate(n, out s2);
			alglib.lincgsolvesparse(s2, A2, true, phi);
			alglib.lincgresults(s2, out phi, out rep2);
		}

		double phi0 = phi[s[0].index];
		for (int i = 0; i < n; i++) {
			phi[i] -= phi0;
		}

		Debug.Log("Distance field calculated (Second linear system), time = " + (Time.realtimeSinceStartup - time)*1000 + "ms");
		time = Time.realtimeSinceStartup;


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

		Debug.Log("Distance gradient calculated, time = " + (Time.realtimeSinceStartup - time)*1000 + "ms");
		time = Time.realtimeSinceStartup;


		// Adjust uv coordinates of vertices in order to show the distance mapping
		// Tangent field provides necessary information for normal mapping
		Vector2[] uv = new Vector2[n];
		Vector4[] tangents = new Vector4[n];
		for (int i = 0; i < n; i++) {
			uv[i] = new Vector2((float) (phi[i] / Settings.mappingDistance), 0);
			Vector3 tgt = new Vector3();
			foreach (Halfedge e in g.vertices[i].edges) {
				if (e.face.index != -1) {
					tgt -= GradPhi[e.face.index];
				}
			}
			tgt.Normalize();
			if (tgt.sqrMagnitude == 0) {
				tgt = g.vertices[i].CalculateNormalTri();
			}
			tangents[i] = new Vector4(tgt.x, tgt.y, tgt.z, 1);

			g.vertices[i].ClearEdgeArray();
		}
		g.linkedMesh.uv = uv;
		g.linkedMesh.tangents = tangents;


		Debug.Log("UV and tangent set, time = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

	}

}
