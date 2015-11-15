using UnityEngine;
using System.Collections;

public class WalkingMan : MonoBehaviour {

	public Geometry land;
	public HeatGeodesics navigation;
	public Face triangle;
	public Vertex[] triVertices;
	public Halfedge[] triEdges;
	public Vector3 normal;
	public Vector3 coeffs;
	public float speed = 0.1f;

	public bool ready = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (ready) {
			GoForward(speed * Time.deltaTime, triangle);
			UpdatePosition();
		}
	}

	public void GetReady(Geometry g, HeatGeodesics nav, Face startSpot) {
		land = g;
		navigation = nav;
		EnterTriangle(startSpot);
		coeffs = new Vector3(0.33f, 0.33f, 0.34f);
		UpdatePosition();
		ready = true;
	}

	void GoForward(float dist, Face lastTriangle, Halfedge avoid = null) {
		if ((navigation.s.p - BarycenterToWorldCoord(coeffs)).magnitude < 0.05f) {
			return;
		}
		Vector3 dir = navigation.X2[triangle.index];
		if (avoid != null) {
			Vector3 e = avoid.vertex.p - avoid.prev.vertex.p;
			dir = Vector3.Project(dir, e).normalized;
		}
		if (dir.sqrMagnitude == 0) {
			return;
		}
		Vector3 gradient = WorldToBarycenterCoord(dir, true);
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
			coeffs += gradient * dist;
		} else {
			Vector3 newPos = BarycenterToWorldCoord(coeffs + gradient * t);
			Face currentTriangle = triangle;
			Face newTriangle = triEdges[(touchBorder + 2) % 3].opposite.face;

			/*if (touchBorder == 0) {
				newTriangle = triEdges[2].opposite.face;
			} else if (touchBorder == 1) {
				newTriangle = triEdges[0].opposite.face;
			} else {
				newTriangle = triEdges[1].opposite.face;
			}*/
			if (newTriangle != lastTriangle && newTriangle.index != -1) {
				EnterTriangle(newTriangle);
				coeffs = WorldToBarycenterCoord(newPos);
				GoForward(dist - t, currentTriangle);
			} else {
				coeffs += gradient * t;
				GoForward(dist - t, currentTriangle, triEdges[(touchBorder + 2) % 3]);
			}
		}
	}

	void EnterTriangle(Face tri) {
		triangle = tri;
		triangle.FillEdgeArray();

		triEdges = triangle.edges.ToArray();
		triVertices = new Vertex[3] {triEdges[0].vertex, triEdges[1].vertex, triEdges[2].vertex};
		normal = triangle.CalculateNormalTri();
		triangle.ClearEdgeArray();
	}
	

	Vector3 BarycenterToWorldCoord(Vector3 coefficients) {
		return triVertices[0].p * coefficients.x + triVertices[1].p * coefficients.y + triVertices[2].p * coefficients.z;
	}

	Vector3 WorldToBarycenterCoord(Vector3 vect, bool isVec = false) {
		if (isVec) {
			vect -= Vector3.Dot(vect, normal) * normal;
		}
		Matrix4x4 m = new Matrix4x4();
		m.SetRow(0, new Vector4(triVertices[0].p.x, triVertices[1].p.x, triVertices[2].p.x, 0));
		m.SetRow(1, new Vector4(triVertices[0].p.y, triVertices[1].p.y, triVertices[2].p.y, 0));
		m.SetRow(2, new Vector4(triVertices[0].p.z, triVertices[1].p.z, triVertices[2].p.z, 0));
		m.SetRow(3, new Vector4(0, 0, 0, 1));
		//Vector3 result = m.inverse.MultiplyVector(vect);
		return m.inverse.MultiplyVector(vect);
	}

	void UpdatePosition() {
		Vector3 pos = BarycenterToWorldCoord(coeffs);
		transform.position = pos;
		Vector3 smoothedNormal = coeffs.x * triVertices[0].CalculateNormalTri() + coeffs.y * triVertices[1].CalculateNormalTri() + coeffs.z * triVertices[2].CalculateNormalTri();
		transform.rotation = Quaternion.LookRotation(smoothedNormal, navigation.X2[triangle.index]);
	}
}
