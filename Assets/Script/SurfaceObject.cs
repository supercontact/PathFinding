using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Allows an object to move on the surface of a mesh, and navigate to the source point when given the gradient of the distance field.
/// </summary>
public class SurfaceObject {

	public Geometry land;
	public HeatGeodesics navigation;

	/// <summary>
	/// The triangle where the object is.
	/// </summary>
	public Face triangle;
	/// <summary>
	/// The 3 vertices of its standing triangle.
	/// </summary>
	public Vertex[] triVertices;
	/// <summary>
	/// The 3 halfedges of its standing triangle.
	/// </summary>
	public Halfedge[] triEdges;
	/// <summary>
	/// The normal of its standing triangle.
	/// </summary>
	public Vector3 normal;
	/// <summary>
	/// The barycentric coordinates of the object related to the 3 vertices of its standing triangle.
	/// </summary>
	public Vector3 coeffs;

	
	public SurfaceObject(Geometry g, HeatGeodesics nav, Face startSpot, Vector3 startPos = default(Vector3)) {
		land = g;
		navigation = nav;
		EnterTriangle(startSpot);
		if (startPos == default(Vector3)) {
			coeffs = new Vector3(0.33f, 0.33f, 0.34f);
		} else {
			coeffs = WorldToBarycenterCoord(startPos, false);
		}
	}

	/// <summary>
	/// Move towards the source over a certain distance.
	/// This method is recursive.
	/// </summary>
	public List<Vector3> GoForward(float dist, int maxIteration = 100, float stopThreshold = 0.05f) {
		List<Vector3> traj = new List<Vector3>();
		GoForward(dist, null, null, maxIteration, stopThreshold, traj);
		return traj;
	}
	private void GoForward(float dist, Face lastTriangle = null, Halfedge avoid = null, int maxIteration = 100, float stopThreshold = 0.05f, List<Vector3> traj = null) {
		if (traj != null && avoid == null) {
			traj.Add(BarycenterToWorldCoord(coeffs) + normal * 0.01f);
		}

		// If too many iteration, stop
		if (maxIteration <= 0) {
			return;
		}
		// If reached the source, stop
		if (Interpolate(navigation.phi, coeffs) < stopThreshold) {
			return;
		}

		// Calculate the moving direction in barycentric form (with a sum of 0)
		Vector3 dir = navigation.GradPhi[triangle.index];
		if (avoid != null) {
			// Avoiding a certain halfedge, in this case, move alongside it.
			Vector3 e = avoid.vertex.p - avoid.prev.vertex.p;
			dir = Vector3.Project(dir, e).normalized;
		}
		if (dir.sqrMagnitude == 0) {
			return;
		}
		Vector3 gradient = WorldToBarycenterCoord(dir, true);

		// Calculate the first border it touches
		int touchBorder = 0;
		float t = (gradient.x < 0 && avoid != triEdges[2]) ? - coeffs.x / gradient.x : float.PositiveInfinity;
		float ty = - coeffs.y / gradient.y;
		if (gradient.y < 0 && ty < t && avoid != triEdges[0]) {
			touchBorder = 1;
			t = ty;
		}
		float tz = - coeffs.z / gradient.z;
		if (gradient.z < 0 && tz < t && avoid != triEdges[1]) {
			touchBorder = 2;
			t = tz;
		}

		if (t >= dist) {
			// The move can be finished in this triangle, not reaching other ones
			coeffs += gradient * dist;
		} else {
			// Moving out of the current triangle
			Vector3 newPos = BarycenterToWorldCoord(coeffs + gradient * t);
			Face currentTriangle = triangle;
			Face newTriangle = triEdges[(touchBorder + 2) % 3].opposite.face;

			if (newTriangle != lastTriangle && newTriangle.index != -1) {
				// Proceed to the next triangle it touches
				EnterTriangle(newTriangle);
				coeffs = WorldToBarycenterCoord(newPos);
				GoForward(dist - t, currentTriangle, null, maxIteration - 1, stopThreshold, traj);
			} else {
				// The triangle it touches is where it came from, or it is the outer boundary.
				// So do not cross the edge, move alongside it
				coeffs += gradient * t;
				GoForward(dist - t, currentTriangle, triEdges[(touchBorder + 2) % 3], maxIteration, stopThreshold, traj);
			}
		}
	}

	/// <summary>
	/// Sets the position to a new triangle, update all related information.
	/// </summary>
	public void EnterTriangle(Face tri) {
		triangle = tri;
		triangle.FillEdgeArray();

		triEdges = triangle.edges.ToArray();
		triVertices = new Vertex[3] {triEdges[0].vertex, triEdges[1].vertex, triEdges[2].vertex};
		normal = triangle.CalculateNormalTri();
		triangle.ClearEdgeArray();
	}
	
	/// <summary>
	/// Transforms the local barycentric coordinates to world 3D coordinates.
	/// </summary>
	public Vector3 BarycenterToWorldCoord(Vector3 coefficients) {
		return triVertices[0].p * coefficients.x + triVertices[1].p * coefficients.y + triVertices[2].p * coefficients.z;
	}

	/// <summary>
	/// Transforms the world 3D coordinates to local barycentric coordinates.
	/// </summary>
	public Vector3 WorldToBarycenterCoord(Vector3 vect, bool isVec = false) {
		// Project to plane
		if (isVec) {
			vect -= Vector3.Dot(vect, normal) * normal;
		} else {
			vect -= Vector3.Dot(vect - triVertices[0].p, normal) * normal;
		}
		// Solve a system of 3 equations
		Matrix4x4 m = new Matrix4x4();
		m.SetRow(0, new Vector4(triVertices[0].p.x, triVertices[1].p.x, triVertices[2].p.x, 0));
		m.SetRow(1, new Vector4(triVertices[0].p.y, triVertices[1].p.y, triVertices[2].p.y, 0));
		m.SetRow(2, new Vector4(triVertices[0].p.z, triVertices[1].p.z, triVertices[2].p.z, 0));
		m.SetRow(3, new Vector4(0, 0, 0, 1));
		return m.inverse.MultiplyVector(vect);
	}

	public double Interpolate(double[] field, Vector3 weights) {
		return field[triVertices[0].index] * weights[0] + field[triVertices[1].index] * weights[1] + field[triVertices[2].index] * weights[2];
	}
}
