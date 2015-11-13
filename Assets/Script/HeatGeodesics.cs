using UnityEngine;
using System.Collections;
/*using com.numericalmethod.suanshu.algebra.linear.vector.doubles;
using com.numericalmethod.suanshu.algebra.linear.vector.doubles.dense;
using com.numericalmethod.suanshu.algebra.linear.matrix.doubles;
using com.numericalmethod.suanshu.algebra.linear.matrix.doubles.matrixtype.sparse;
using com.numericalmethod.suanshu.algebra.linear.matrix.doubles.matrixtype.sparse.solver.iterative;
using com.numericalmethod.suanshu.algebra.linear.matrix.doubles.matrixtype.sparse.solver.iterative.nonstationary;
using com.numericalmethod.suanshu.algebra.linear.matrix.doubles.factorization.triangle;
using com.numericalmethod.suanshu.algebra.linear.matrix.doubles.linearsystem;
using com.numericalmethod.suanshu.misc.algorithm.iterative.tolerance;
using com.numericalmethod.suanshu.algebra.linear.matrix.doubles.factorization.triangle.cholesky;*/

public class HeatGeodesics {

	public Geometry g;
	public alglib.sparsematrix A1, A2;
	//public SparseMatrix A1, A2;
	//public Matrix L1, L2;

	//public LSProblem P1, P2;
	public double[] u;
	public Vector3[] X;
	public double[] divX;
	public double[] phi;
	public Vector3[] X2;

	public Vertex s;

	public HeatGeodesics(Geometry g) {
		this.g = g;
	}

	public void Initialize() {
		int n = g.vertices.Count;
		A1 = g.CalculateLcMatrixSparse(- g.h * g.h);
		for (int i = 0; i < n; i++) {
			alglib.sparseadd(A1, i, i, g.vertices[i].CalculateVertexAreaTri());
			//A1.set(i + 1, i + 1, A1.get(i + 1, i + 1) + g.vertices[i].CalculateVertexAreaTri());
		}
		A2 = g.CalculateLcMatrixSparse(-1);
		for (int i = 0; i < n; i++) {
			//alglib.sparseadd(A2, i, i, 0.1);
			//A1.set(i + 1, i + 1, A1.get(i + 1, i + 1) + g.vertices[i].CalculateVertexAreaTri());
		}
	
		alglib.sparseconverttocrs(A1);
		alglib.sparseconverttocrs(A2);
		//A1 = new CSRSparseMatrix(A1);
		//A2 = new CSRSparseMatrix(A2);
	}

	public void CalculateGeodesics(Vertex source) {
		s = source;
		float time = Time.realtimeSinceStartup;
		Debug.Log("t = 0ms");

		// Put the mark at the source vertex
		GameObject pin = GameObject.Find("Pin");
		pin.transform.position = source.p;
		pin.transform.rotation = Quaternion.LookRotation(source.CalculateNormalTri());

		// First equation
		int n = g.vertices.Count;
		int f = g.faces.Count;
		double[] b = new double[n];
		b[source.index] = 1;

		alglib.lincgstate s1;
		alglib.lincgreport rep1;
		alglib.lincgcreate(n, out s1);
		alglib.lincgsolvesparse(s1, A1, true, b);
		alglib.lincgresults(s1, out u, out rep1);

		/*Vector b = new DenseVector(n, 0);
		b.set(source.index + 1, 1);
		LSProblem P1 = new LSProblem(A1, b);
		ConjugateGradientSolver solver = new ConjugateGradientSolver(100, new AbsoluteTolerance());
		IterativeLinearSystemSolver.Solution sol1 = solver.solve(P1);
		double[] u = sol1.search(new DenseVector(n)).toArray();*/

		Debug.Log("Solved first linear system, termination = " + rep1.terminationtype);
		//Debug.Log("Solved first linear system");
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

		// Visualize heat field
		/*Color[] colors = new Color[n];
		for (int i = 0; i < n; i++) {
			colors[i] = Color.Lerp(Color.blue, Color.white, (float) (0.05 * System.Math.Log(u[i])+1));
		}
		g.linkedMesh.colors = colors;*/

		// Calculate X, normalized gradient of heat flow
		//GameObject visual = GameObject.Find("FlowVisualization");
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

			//Visualize flow
			/*GameObject stick = GameObject.Instantiate(GameObject.Find("Edge"));
			stick.transform.SetParent(visual.transform);
			stick.transform.position = face.CalculateCenter();
			stick.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
			stick.transform.rotation = Quaternion.LookRotation(X[i], face.CalculateNormalTri());*/
		}

		Debug.Log("Gradient of heat flow calculated");
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

		// Calculate div(X);
		divX = new double[n];
		for (int i = 0; i < n; i++) {
			g.vertices[i].FillEdgeArray();
			Vector3 v = g.vertices[i].p;
			foreach (Halfedge edge in g.vertices[i].edges) {
				double limit = 10000;
				Vector3 v1 = edge.prev.vertex.p;
				Vector3 v2 = edge.next.vertex.p;
				double cos1 = Vector3.Dot(v-v2, v1-v2) / (v-v2).magnitude / (v1-v2).magnitude;
				double cot1 = cos1 / System.Math.Sqrt(1 - cos1 * cos1);
				if (double.IsNaN(cot1) || System.Math.Abs(cot1) > limit) {
					cot1 = cos1 > 0 ? limit : -limit;
				}
				double cos2 = Vector3.Dot(v-v1, v2-v1) / (v-v1).magnitude / (v2-v1).magnitude;
				double cot2 = cos2 / System.Math.Sqrt(1 - cos2 * cos2);
				if (double.IsNaN(cot2) || System.Math.Abs(cot2) > limit) {
					cot2 = cos2 > 0 ? limit : -limit;
				}
				divX[i] += cot1 * Vector3.Dot(v1 - v, X[edge.face.index]) + cot2 * Vector3.Dot(v2 - v, X[edge.face.index]);
			}
			divX[i] /= -2; //inverse
		}

		Debug.Log("Div of X calculated");
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");

		/*alglib.linlsqrstate s2;
		alglib.linlsqrreport rep2;
		alglib.linlsqrcreate(n, n, out s2);
		alglib.linlsqrsolvesparse(s2, A2, divX);
		alglib.linlsqrresults(s2, out phi, out rep2);*/

		alglib.lincgstate s2;
		alglib.lincgreport rep2;
		alglib.lincgcreate(n, out s2);
		alglib.lincgsolvesparse(s2, A2, true, divX);
		alglib.lincgresults(s2, out phi, out rep2);

		/*Vector b2 = new DenseVector(divX);
		LSProblem P2 = new LSProblem(A2, b2);
		ConjugateGradientSolver solver2 = new ConjugateGradientSolver(200, new AbsoluteTolerance(0.0005));
		IterativeLinearSystemSolver.Solution sol2 = solver2.solve(P2);
		double[] phi = sol2.search(new DenseVector(n)).toArray();*/

		double phi0 = phi[source.index];
		double maxDistance = 4;
		for (int i = 0; i < n; i++) {
			phi[i] -= phi0;
			/*if (phi[i] > maxDistance) {
				maxDistance = phi[i];
			}*/
		}


		Debug.Log("Distance field calculated (Second linear system), termination = " + rep2.terminationtype);
		//Debug.Log("Distance field calculated (Second linear system)");
		Debug.Log("t = " + (Time.realtimeSinceStartup - time)*1000 + "ms");


		Vector2[] uv = new Vector2[n];
		Vector4[] tangents = new Vector4[n];
		for (int i = 0; i < n; i++) {
			uv[i] = new Vector2((float) (phi[i] / maxDistance), 0);
			Vector3 tgt = new Vector3();
			foreach (Halfedge e in g.vertices[i].edges) {
				tgt += X[e.face.index];
			}
			tgt.Normalize();
			tangents[i] = new Vector4(tgt.x, tgt.y, tgt.z, 1);
			g.vertices[i].ClearEdgeArray();
		}
		g.linkedMesh.uv = uv;
		g.linkedMesh.tangents = tangents;


		X2 = new Vector3[f];
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
