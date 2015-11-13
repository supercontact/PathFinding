using UnityEngine;
using System.Collections;

public class TestPathFinding : MonoBehaviour {

	Camera cam;
	Geometry g;
	HeatGeodesics hg;
	GameObject earth;
	// Use this for initialization
	void Start () {
		cam = GameObject.Find("Main Camera").GetComponent<Camera>();

		earth = GameObject.Find("Earth");
		//earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.CreateSphere(1, 48);
		earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/high_genus.off", 0.2f);
		//earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/cow.off", 2);
		//earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/bague.off", 1);
		//earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/letter_a.off", 3);
		//earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/tri_triceratops.off", 0.2f);

		earth.AddComponent<MeshCollider>().sharedMesh = earth.GetComponent<MeshFilter>().sharedMesh;

		SetTexture(0);

		/*PathFinding pf = new PathFinding();
		Geometry g = new Geometry(GetComponent<MeshFilter>().mesh);
		pf.ShortestPathSimple(g, g.vertices[42]);
		pf.DrawPathFrom(g.vertices[805]);
		pf.DrawAllBorder();*/

		g = new Geometry(GetComponent<MeshFilter>().sharedMesh);
		/*g.FixVertex(758);
		g.FixVertex(295);
		g.FixVertex(395);
		g.FixVertex(2449);*/
		hg = new HeatGeodesics(g);
		hg.Initialize();
		hg.CalculateGeodesics(g.vertices[42]);

		WalkingMan man = GameObject.Find("LittleMan").GetComponent<WalkingMan>();
		man.GetReady(g, hg, g.faces[42]);
	}

	float offset = 0;
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown(0)) {
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			RaycastHit info;
			if (Physics.Raycast(ray, out info)) {
				Debug.Log("triangle hit = " + info.triangleIndex);
				if (info.triangleIndex != -1) {
					Face f = g.faces[info.triangleIndex];
					f.FillEdgeArray();
					Vertex v = f.edges[0].vertex;
					if ((f.edges[1].vertex.p - info.point).sqrMagnitude < (v.p - info.point).sqrMagnitude)
						v = f.edges[1].vertex;
					if ((f.edges[2].vertex.p - info.point).sqrMagnitude < (v.p - info.point).sqrMagnitude)
						v = f.edges[2].vertex;
					f.ClearEdgeArray();
					Debug.Log("vertex hit = " + v.index);
					hg.CalculateGeodesics(v);
				}
			}
		}

		offset -= 0.05f * Time.deltaTime;
		//earth.GetComponent<MeshRenderer>().material.mainTextureOffset -= new Vector2(0.05f, 0) * Time.deltaTime;
		earth.GetComponent<MeshRenderer>().material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
	}

	void SetTexture(int type) {
		ColorMixer background;
		ColorMixer stripe;
		Texture2D tex;
		Material mat = earth.GetComponent<MeshRenderer>().material;

		switch (type) {

		case 0:
		default:
			background = new ColorMixer();
			stripe = new ColorMixer();

			background.InsertColorNode(Color.yellow, 0.1f);
			background.InsertColorNode(Color.red, 0.3f);
			background.InsertColorNode(Color.black, 0.5f);
			background.InsertColorNode(Color.black, 0.6f);
			background.InsertColorNode(Color.red, 0.8f);
			background.InsertColorNode(Color.yellow, 1f);
			stripe.InsertColorNode(new Color(1,1,0.5f), 0);
			tex = MeshFactory.CreateStripedTexture(1024, 30, 4, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 1f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0f), 1);
			tex = MeshFactory.CreateStripedTexture(1024, 30, 4, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");
			break;

		case 1:
			background = new ColorMixer();
			stripe = new ColorMixer();

			background.InsertColorNode(Color.white, 0f);
			background.InsertColorNode(new Color(0.5f, 0.75f, 1f), 0.5f);
			background.InsertColorNode(Color.blue, 1);
			stripe.InsertColorNode(new Color(0.5f,0.5f,0.5f), 0);
			tex = MeshFactory.CreateStripedTexture(1024, 40, 8, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0.43f, 0.43f, 0.43f, 0.6f), 0);
			stripe.InsertColorNode(new Color(0.9f, 0.9f, 0.9f, 0.85f), 0);
			tex = MeshFactory.CreateStripedTexture(1024, 40, 8, 20, background, stripe);
			mat.SetTexture("_SpecGlossMap", tex);
			mat.EnableKeyword("_SPECGLOSSMAP");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.25f), 0);
			//stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.2f), 0.49f);
			//stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.8f), 0.51f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.75f), 1);
			tex = MeshFactory.CreateStripedTexture(1024, 40, 8, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");
			break;

		case 2:
			background = new ColorMixer();
			stripe = new ColorMixer();
			
			background.InsertColorNode(new Color(0.081f, 0.1137f, 0.06f), 0f);
			stripe.InsertColorNode(new Color(0f, 0f, 0f), 0);
			tex = MeshFactory.CreateStripedTexture(1024, 60, 8, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0f, 0f, 0f, 0f), 0);
			stripe.InsertColorNode(new Color(0.6f, 1f, 0.4f, 1f), 0);
			tex = MeshFactory.CreateStripedTexture(1024, 60, 6, 21, background, stripe);
			mat.SetTexture("_EmissionMap", tex);
			mat.SetColor("_EmissionColor", new Color(0.9f, 0.9f, 0.9f));
			mat.EnableKeyword("_EMISSION");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 1f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.15f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.85f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0f), 1);
			tex = MeshFactory.CreateStripedTexture(1024, 60, 8, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");
			break;

		case 3:
			background = new ColorMixer();
			stripe = new ColorMixer();
			
			background.InsertColorNode(new Color(1f, 0.7f, 0.30f), 0f);
			stripe.InsertColorNode(new Color(0.125f, 0.125f, 0.1f), 0);
			tex = MeshFactory.CreateStripedTexture(1024, 60, 8, 21, background, stripe);
			mat.mainTexture = tex;

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.70f, 0.30f, 0.75f), 0);
			stripe.InsertColorNode(new Color(0.3f, 0.3f, 0.3f, 0.4f), 0);
			tex = MeshFactory.CreateStripedTexture(1024, 60, 8, 21, background, stripe);
			mat.SetTexture("_SpecGlossMap", tex);
			mat.EnableKeyword("_SPECGLOSSMAP");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0f, 0f, 0f, 0f), 0);
			stripe.InsertColorNode(new Color(1f, 1f, 0.8f, 1f), 0);
			tex = MeshFactory.CreateStripedTexture(1024, 60, 8, 21, background, stripe);
			mat.SetTexture("_EmissionMap", tex);
			mat.EnableKeyword("_EMISSION");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 1f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.15f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.85f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0f), 1);
			tex = MeshFactory.CreateStripedTexture(1024, 60, 10, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");
			break;
		}
	}

}
