using UnityEngine;
using System.Collections;

public class TestPathFinding : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GameObject earth = GameObject.Find("Earth");
		earth.GetComponent<MeshFilter>().mesh = MeshFactory.CreateSphere(1, 24);
		//earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/high_genus.off");
		//earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/cow.off");

		ColorMixer background = new ColorMixer();
		ColorMixer stripe = new ColorMixer();
		background.InsertColorNode(Color.yellow, 0);
		background.InsertColorNode(Color.red, 0.5f);
		background.InsertColorNode(Color.black, 1);
		stripe.InsertColorNode(new Color(1,1,0.5f), 0);
		Texture2D stripes = MeshFactory.CreateStripedTexture(1024, 40, 5, 20, background, stripe);
		earth.GetComponent<MeshRenderer>().material.mainTexture = stripes;

		/*PathFinding pf = new PathFinding();
		Geometry g = new Geometry(GetComponent<MeshFilter>().mesh);
		pf.ShortestPathSimple(g, g.vertices[42]);
		pf.DrawPathFrom(g.vertices[805]);
		pf.DrawAllBorder();*/

		Geometry g = new Geometry(GetComponent<MeshFilter>().mesh);
		HeatGeodesics hg = new HeatGeodesics(g);
		hg.Initialize();
		hg.CalculateGeodesics(g.vertices[42]);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
