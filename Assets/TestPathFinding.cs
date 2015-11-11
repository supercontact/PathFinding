using UnityEngine;
using System.Collections;

public class TestPathFinding : MonoBehaviour {

	Camera camera;
	Geometry g;
	HeatGeodesics hg;
	// Use this for initialization
	void Start () {
		camera = GameObject.Find("Main Camera").GetComponent<Camera>();

		GameObject earth = GameObject.Find("Earth");
		//earth.GetComponent<MeshFilter>().mesh = MeshFactory.CreateSphere(1, 48);
		earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/high_genus.off");
		//earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/cow.off");

		earth.AddComponent<MeshCollider>().sharedMesh = earth.GetComponent<MeshFilter>().sharedMesh;

		ColorMixer background = new ColorMixer();
		ColorMixer stripe = new ColorMixer();
		background.InsertColorNode(Color.yellow, 0);
		background.InsertColorNode(Color.red, 0.5f);
		background.InsertColorNode(Color.black, 1);
		stripe.InsertColorNode(new Color(1,1,0.5f), 0);
		Texture2D stripes = MeshFactory.CreateStripedTexture(1024, 40, 5, 20, background, stripe);
		earth.GetComponent<MeshRenderer>().material.mainTexture = stripes;

		background = new ColorMixer();
		stripe = new ColorMixer();
		background.InsertColorNode(new Color(0.5f, 0.5f, 0.5f, 0.6f), 0);
		stripe.InsertColorNode(new Color(0.5f, 0.5f, 0.5f, 0.6f), 0);
		Texture2D stripeSmoothness = MeshFactory.CreateStripedTexture(1024, 40, 5, 20, background, stripe);
		earth.GetComponent<MeshRenderer>().material.SetTexture("_SpecGlossMap", stripeSmoothness);

		/*PathFinding pf = new PathFinding();
		Geometry g = new Geometry(GetComponent<MeshFilter>().mesh);
		pf.ShortestPathSimple(g, g.vertices[42]);
		pf.DrawPathFrom(g.vertices[805]);
		pf.DrawAllBorder();*/

		g = new Geometry(GetComponent<MeshFilter>().mesh);
		hg = new HeatGeodesics(g);
		hg.Initialize();
		hg.CalculateGeodesics(g.vertices[42]);
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown(0)) {
			Ray ray = camera.ScreenPointToRay(Input.mousePosition);
			RaycastHit info;
			if (Physics.Raycast(ray, out info)) {
				Debug.Log("triangle hit = " + info.triangleIndex);
				Face f = g.faces[info.triangleIndex];
				f.FillEdgeArray();
				Vertex v = f.edges[0].vertex;
				if ((f.edges[1].vertex.p - info.point).sqrMagnitude < (v.p - info.point).sqrMagnitude)
					v = f.edges[1].vertex;
				if ((f.edges[2].vertex.p - info.point).sqrMagnitude < (v.p - info.point).sqrMagnitude)
					v = f.edges[2].vertex;
				f.ClearEdgeArray();
				hg.CalculateGeodesics(v);
			}
		}
	}
}
