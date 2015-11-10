using UnityEngine;
using System.Collections;

public class TestPathFinding : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GameObject earth = GameObject.Find("Earth");
		earth.GetComponent<MeshFilter>().mesh = MeshFactory.CreateSphere(1, 24);

		/*PathFinding pf = new PathFinding();
		Geometry g = new Geometry(GetComponent<MeshFilter>().mesh);
		pf.ShortestPathSimple(g, g.vertices[42]);
		pf.DrawPathFrom(g.vertices[805]);
		pf.DrawAllBorder();*/

		Geometry g = new Geometry(GetComponent<MeshFilter>().mesh);
		HeatGeodesics hg = new HeatGeodesics(g);
		hg.Initialize();
		hg.CalculateGeodesics(g.vertices[242]);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
