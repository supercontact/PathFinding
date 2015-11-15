using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TestPathFinding : MonoBehaviour {

	Camera cam;
	Geometry g;
	HeatGeodesics hg;
	GameObject earth;
	WalkingMan man;
	Text counter;
	int textureIndex = 0;
	bool firstClick = true;

	// Use this for initialization
	void Start () {
		cam = GameObject.Find("Main Camera").GetComponent<Camera>();
		man = GameObject.Find("LittleMan").GetComponent<WalkingMan>();
		earth = GameObject.Find("Earth");
		counter = GameObject.Find("Counter").GetComponent<Text>();

		SetLevel(1);


		/*PathFinding pf = new PathFinding();
		Geometry g = new Geometry(GetComponent<MeshFilter>().mesh);
		pf.ShortestPathSimple(g, g.vertices[42]);
		pf.DrawPathFrom(g.vertices[805]);
		pf.DrawAllBorder();*/

	}

	float offset = 0;

	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown(0)) {
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			RaycastHit info;
			if (Physics.Raycast(ray, out info)) {
				//Debug.Log("triangle hit = " + info.triangleIndex);
				if (info.triangleIndex != -1) {
					if (firstClick) {
						GameObject.Find("Tutorial").SetActive(false);
						firstClick = false;
					}

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
		if (Input.GetKey(KeyCode.Escape)) {
			Application.Quit();
		}

		offset -= Constant.ScrollSpeed * Time.deltaTime;
		//earth.GetComponent<MeshRenderer>().material.mainTextureOffset -= new Vector2(0.05f, 0) * Time.deltaTime;
		earth.GetComponent<MeshRenderer>().material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
	}

	public void SetLevel(int level) {

		switch (level) {
		case 0:
		default:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/high_genus.off", 0.2f, new Vector3(0, 0, -0.6f));
			SetTexture(2);
			Constant.cotLimit = 10000;
			Constant.tFactor = 1;
			break;
		case 1:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/bague.off", 0.5f);
			SetTexture(0);
			Constant.cotLimit = 10000;
			Constant.tFactor = 1;
			break;
		case 2:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/cow.off", 1.5f, new Vector3(0.15f, 0.15f, 0));
			SetTexture(3);
			Constant.cotLimit = 10000;
			Constant.tFactor = 1;
			break;
		case 3:
		case 6:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/tri_triceratops.off", 0.2f, new Vector3(0.15f, 0, 0));
			SetTexture(4);
			Constant.cotLimit = 5;
			Constant.tFactor = 1;
			break;
		case 4:
			//earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/bun_zipper.off", 12f, new Vector3(0.2f, -1f, 0));
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/horse1.off", 12f, new Vector3(0f, 0f, -0.5f), Quaternion.Euler(-90, 0, 0));
			SetTexture(1);
			Constant.cotLimit = 10000;
			Constant.tFactor = 10;
			break;
		case 5:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.CreateSphere(1, 48);
			SetTexture(0);
			Constant.cotLimit = 10000;
			Constant.tFactor = 1;
			break;
		}

		earth.GetComponent<MeshCollider>().sharedMesh = earth.GetComponent<MeshFilter>().sharedMesh;
		g = new Geometry(GetComponent<MeshFilter>().sharedMesh);
		counter.text = "Triangle Count = " + g.faces.Count;

		if (level == 6) {
			g.FixVertex(758);
			g.FixVertex(295);
			g.FixVertex(395);
			g.FixVertex(2449);
		}
		hg = new HeatGeodesics(g);
		hg.Initialize();
		hg.CalculateGeodesics(g.vertices[69]);
		man.GetReady(g, hg, g.faces[42]);
	}

	public void SetTexture(int type) {
		textureIndex = type;

		ColorMixer background;
		ColorMixer stripe;
		Texture2D tex;
		Material mat = earth.GetComponent<MeshRenderer>().material;

		int period;
		int width;

		switch (type) {

		case 0:
		default:
			background = new ColorMixer();
			stripe = new ColorMixer();
			period = 60;
			width = 8;

			background.InsertColorNode(Color.yellow, 0.1f);
			background.InsertColorNode(Color.red, 0.3f);
			background.InsertColorNode(Color.black, 0.5f);
			background.InsertColorNode(Color.black, 0.6f);
			background.InsertColorNode(Color.red, 0.8f);
			background.InsertColorNode(Color.yellow, 1f);
			stripe.InsertColorNode(new Color(1,1,0.5f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 1f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");

			mat.DisableKeyword("_SPECGLOSSMAP");
			mat.DisableKeyword("_EMISSION");
			break;

		case 1:
			background = new ColorMixer();
			stripe = new ColorMixer();
			period = 60;
			width = 8;

			background.InsertColorNode(Color.white, 0.1f);
			background.InsertColorNode(new Color(0.5f, 0.75f, 1f), 0.3f);
			background.InsertColorNode(Color.blue, 0.5f);
			background.InsertColorNode(Color.blue, 0.6f);
			background.InsertColorNode(new Color(0.5f, 0.75f, 1f), 0.8f);
			background.InsertColorNode(Color.white, 1f);
			stripe.InsertColorNode(new Color(0.5f,0.5f,0.5f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0.43f, 0.43f, 0.43f, 0.6f), 0);
			stripe.InsertColorNode(new Color(0.9f, 0.9f, 0.9f, 0.85f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.SetTexture("_SpecGlossMap", tex);
			mat.EnableKeyword("_SPECGLOSSMAP");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.25f), 0);
			//stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.2f), 0.49f);
			//stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.8f), 0.51f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.75f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");

			mat.DisableKeyword("_EMISSION");
			break;

		case 2:
			background = new ColorMixer();
			stripe = new ColorMixer();
			period = 60;
			width = 8;
			
			background.InsertColorNode(new Color(0.081f, 0.1137f, 0.06f), 0f);
			stripe.InsertColorNode(new Color(0f, 0f, 0f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0f, 0f, 0f, 0f), 0);
			stripe.InsertColorNode(new Color(0.6f, 1f, 0.4f, 1f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
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
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");

			mat.DisableKeyword("_SPECGLOSSMAP");
			break;

		case 3:
			background = new ColorMixer();
			stripe = new ColorMixer();
			period = 60;
			width = 10;
			
			background.InsertColorNode(new Color(1f, 0.7f, 0.30f), 0f);
			stripe.InsertColorNode(new Color(0.125f, 0.125f, 0.1f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.mainTexture = tex;

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.70f, 0.30f, 0.75f), 0);
			stripe.InsertColorNode(new Color(0.3f, 0.3f, 0.3f, 0.4f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.SetTexture("_SpecGlossMap", tex);
			mat.EnableKeyword("_SPECGLOSSMAP");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0f, 0f, 0f, 0f), 0);
			stripe.InsertColorNode(new Color(1f, 1f, 0.8f, 1f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.SetTexture("_EmissionMap", tex);
			mat.SetColor("_EmissionColor", new Color(0f, 0f, 0f));
			mat.EnableKeyword("_EMISSION");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 1f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.15f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.85f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");
			break;

		case 4:
			background = new ColorMixer();
			stripe = new ColorMixer();
			period = 60;
			width = 10;
			
			background.InsertColorNode(new Color(0.25f, 0.25f, 0.25f), 0f);
			stripe.InsertColorNode(new Color(0.05f, 0.05f, 0.05f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0f, 0f, 0f, 0.25f), 0);
			stripe.InsertColorNode(new Color(0.25f, 0.25f, 0.25f, 0.5f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.SetTexture("_SpecGlossMap", tex);
			mat.EnableKeyword("_SPECGLOSSMAP");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0f, 0f, 0f, 0f), 0);
			stripe.InsertColorNode(new Color(0.85f, 0.7f, 0.1f, 1f), 0.1f);
			stripe.InsertColorNode(new Color(1f, 0.05f, 0f, 1f), 0.3f);
			stripe.InsertColorNode(new Color(0f, 0f, 0f, 1f), 0.5f);
			stripe.InsertColorNode(new Color(0f, 0f, 0f, 1f), 0.6f);
			stripe.InsertColorNode(new Color(1f, 0.05f, 0f, 1f), 0.8f);
			stripe.InsertColorNode(new Color(0.85f, 0.7f, 0.1f, 1f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.SetTexture("_EmissionMap", tex);
			mat.SetColor("_EmissionColor", new Color(1f, 1f, 1f));
			mat.EnableKeyword("_EMISSION");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 1f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.15f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.85f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");
			break;
		}
	}

}
