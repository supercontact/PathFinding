using UnityEngine;
using System.Collections;

/// <summary>
/// A little man who walks on the surface of a mesh, always towards the given source point.
/// </summary>
public class WalkingMan : MonoBehaviour {

	public SurfaceObject coord;

	public float speed = 0.5f;
	public bool ready = false;
	

	// Update is called once per frame
	void Update () {
		if (ready) {
			coord.GoForward(speed * Time.deltaTime);
			UpdatePosition();
		}
	}

	/// <summary>
	/// Begins to walk on a mesh providing the geodesics.
	/// Begins at the center of the triangle startSpot.
	/// </summary>
	public void GetReady(Geometry g, HeatGeodesics nav, Face startSpot) {
		coord = new SurfaceObject(g, nav, startSpot);
		UpdatePosition();
		ready = true;
	}

	/// <summary>
	/// Updates the man's position and rotation according to its barycentric coordinates and the surface information.
	/// </summary>
	void UpdatePosition() {
		Vector3 pos = coord.BarycenterToWorldCoord(coord.coeffs);
		transform.position = pos;
		Vector3 smoothedNormal = coord.coeffs.x * coord.triVertices[0].CalculateNormalTri() 
			+ coord.coeffs.y * coord.triVertices[1].CalculateNormalTri() 
			+ coord.coeffs.z * coord.triVertices[2].CalculateNormalTri();
		transform.rotation = Quaternion.LookRotation(smoothedNormal, coord.navigation.GradPhi[coord.triangle.index]);
	}
}
