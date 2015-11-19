using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// The main program.
/// </summary>
public class TestPathFinding : MonoBehaviour {

	// Public variables that are linked via Unity inspector
	public Camera cam;
	public GameObject earth;
	public WalkingMan man;
	public GameObject pin;
	public Mesh road;
	public GameObject roadBase;
	public Text triangleCounter;

	Geometry g;
	HeatGeodesics hg;

	bool firstClick = true;
	int textureIndex;
	float t;
	float offset = 0;

	// Start is called at the beginng
	void Start () {
		road = Instantiate<Mesh>(road);
		MeshFactory.TransformMesh(road, Vector3.zero, Quaternion.Euler(-180, 0, 0));

		SetLevel(1);

		/*PathFinding pf = new PathFinding();
		Geometry g = new Geometry(GetComponent<MeshFilter>().mesh);
		pf.ShortestPathSimple(g, g.vertices[42]);
		pf.DrawPathFrom(g.vertices[805]);
		pf.DrawAllBorder();*/
	}

	// Update is called once per frame
	void Update () {

		// When clicking on the surface of the mesh, reset source
		if (Input.GetMouseButtonDown(0)) {
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			RaycastHit info;
			if (Physics.Raycast(ray, out info)) {
				Debug.Log("triangle hit = " + info.triangleIndex);
				if (info.triangleIndex != -1) {
					Face f = g.faces[info.triangleIndex];

					// Find the closest vertex to the mouse in the hit triangle
					f.FillEdgeArray();
					Vertex v = f.edges[0].vertex;
					if ((f.edges[1].vertex.p - info.point).sqrMagnitude < (v.p - info.point).sqrMagnitude)
						v = f.edges[1].vertex;
					if ((f.edges[2].vertex.p - info.point).sqrMagnitude < (v.p - info.point).sqrMagnitude)
						v = f.edges[2].vertex;
					f.ClearEdgeArray();
					Debug.Log("vertex hit = " + v.index);

					// Set the source
					hg.CalculateGeodesics(v);

					// Put the mark at the source vertex
					pin.transform.position = v.p;
					pin.transform.rotation = Quaternion.LookRotation(v.CalculateNormalTri());

					// Hide tutorial text
					if (firstClick) {
						GameObject.Find("Tutorial").SetActive(false);
						firstClick = false;
					}
				}
			}
		}

		// Scrolling the surface texture
		Material mat = earth.GetComponent<MeshRenderer>().material;
		offset -= Settings.ScrollSpeed * Time.deltaTime;
		mat.mainTextureOffset = new Vector2(offset, 0);

		// Emission texture animation
		if (textureIndex == 2) {
			float tPhase = 3f;
			float tBlink = 0.10f;
			float c;
			t = (t + Time.deltaTime) % (tPhase * 2);
			if (t < tPhase - 3 * tBlink) {
				Settings.ScrollSpeed = 0.08f;
				mat.SetColor("_EmissionColor", new Color(0.9f, 0.9f, 0.9f));
			} else if (t < tPhase - 2 * tBlink) {
				c = 0.9f * (0.3f + 1.4f * Mathf.Abs(t - tPhase + 2.5f * tBlink) / tBlink);
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			} else if (t < tPhase - tBlink) {
				c = 0.9f * (0.3f + 1.4f * Mathf.Abs(t - tPhase + 1.5f * tBlink) / tBlink);
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			} else if (t < tPhase) {
				c = 0.9f * (tPhase - t) / tBlink;
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			} else if (t < 2 * tPhase - 3 * tBlink) {
				Settings.ScrollSpeed = 0.04f;
				mat.SetColor("_EmissionColor", new Color(0, 0, 0));
			} else if (t < 2 * tPhase - 2 * tBlink) {
				c = 0.9f * (0.5f - Mathf.Abs(t - 2 *tPhase + 2.5f * tBlink) / tBlink);
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			} else if (t < 2 * tPhase - tBlink) {
				c = 0.9f * (0.5f - Mathf.Abs(t - 2 *tPhase + 1.5f * tBlink) / tBlink);
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			} else {
				Settings.ScrollSpeed = 0.08f;
				c = 0.9f * (t - 2 * tPhase + tBlink) / tBlink;
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			}
		} else if (textureIndex == 3) {
			float tPeriod = 7f;
			float tLight = 3f;
			float c;
			t = (t + Time.deltaTime) % tPeriod;
			if (t < tLight) {
				float cos = Mathf.Cos(Mathf.PI * (t - tLight / 2) / tLight);
				c = 0.7f * cos;
				Settings.ScrollSpeed = 0.05f * (1 - 0.9f * cos);
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			} else  {
				mat.SetColor("_EmissionColor", new Color(0f, 0f, 0f));
			}
		}

		// Quit the application
		if (Input.GetKey(KeyCode.Escape)) {
			Application.Quit();
		}
	}

	/// <summary>
	/// Load a certain mesh.
	/// </summary>
	public void SetLevel(int level) {

		Settings.tFactor = 1;
		Settings.cotLimit = 10000;

		switch (level) {
		case 0:
		default:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/high_genus", 0.2f, new Vector3(0, 0, -0.6f));
			SetTexture(2);
			Settings.defaultSource = 94;
			Settings.initialManPos = 1947;
			break;
		case 1:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/bague", 0.5f);
			SetTexture(0);
			Settings.defaultSource = 175;
			Settings.initialManPos = 230;
			break;
		case 2:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/cow", 1.5f, new Vector3(0.15f, 0.15f, 0));
			SetTexture(3);
			Settings.defaultSource = 429;
			Settings.initialManPos = 2129;
			break;
		case 3:
		case 6:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/tri_triceratops", 0.2f, new Vector3(0.15f, 0, 0));
			SetTexture(4);
			Settings.cotLimit = 5;
			Settings.defaultSource = 42;
			Settings.initialManPos = 918;
			break;
		case 4:
			//earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/bun_zipper.off", 12f, new Vector3(0.2f, -1f, 0));
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.ReadMeshFromFile("OFF/horse1", 12f, new Vector3(0f, 0f, -0.3f), Quaternion.Euler(-90, 0, 0));
			SetTexture(5);
			Settings.tFactor = 10;
			Settings.defaultSource = 1559;
			Settings.initialManPos = 31107;
			break;
		case 5:
			earth.GetComponent<MeshFilter>().sharedMesh = MeshFactory.CreateSphere(1, 48);
			SetTexture(0);
			Settings.defaultSource = 42;
			Settings.initialManPos = 42;
			break;
		case 7:
			earth.GetComponent<MeshFilter>().sharedMesh = road;
			SetTexture(1);
			Settings.tFactor = 20;
			Settings.defaultSource = 285;
			Settings.initialManPos = 883;
			break;
		}
		roadBase.SetActive(level == 7);

		// Set collider to detect mouse hit
		earth.GetComponent<MeshCollider>().sharedMesh = earth.GetComponent<MeshFilter>().sharedMesh;

		// Build geometry data
		g = new Geometry(GetComponent<MeshFilter>().sharedMesh);

		if (level == 6) {
			// Fix certain broken triangles for the triceratops
			g.FixVertex(758);
			g.FixVertex(295);
			g.FixVertex(395);
			g.FixVertex(2449);
		}

		// Start heat method
		hg = new HeatGeodesics(g);
		hg.Initialize();
		hg.CalculateGeodesics(g.vertices[Settings.defaultSource]);
		if (man.isActiveAndEnabled) {
			man.GetReady(g, hg, g.faces[Settings.initialManPos]);
		}

		// Put the mark at the source vertex
		pin.transform.position = g.vertices[Settings.defaultSource].p;
		pin.transform.rotation = Quaternion.LookRotation(g.vertices[Settings.defaultSource].CalculateNormalTri());

		// Update counter
		triangleCounter.text = "Triangle Count = " + g.faces.Count;
	}

	/// <summary>
	/// Generate and add texture to the mesh.
	/// </summary>
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
			// Yellow-red-black rubber, bright yellow gap
			period = 82;
			width = 11;
			Settings.mappingDistance = 3;
			Settings.ScrollSpeed = 0.066f;

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(Color.yellow, 0.1f);
			background.InsertColorNode(Color.red, 0.4f);
			background.InsertColorNode(Color.black, 0.7f);
			background.InsertColorNode(Color.black, 0.8f);
			background.InsertColorNode(Color.red, 0.9f);
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
			// White-blue rubber, silver metal wire
			period = 60;
			width = 8;
			Settings.mappingDistance = 4;
			Settings.ScrollSpeed = 0.05f;

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(Color.white, 0.0f);
			background.InsertColorNode(new Color(0.5f, 0.75f, 1f), 0.3f);
			background.InsertColorNode(Color.blue, 0.7f);
			background.InsertColorNode(Color.blue, 0.8f);
			background.InsertColorNode(new Color(0.5f, 0.75f, 1f), 0.9f);
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
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.75f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");

			mat.DisableKeyword("_EMISSION");
			break;

		case 2:
			// Dark green shell, light green fluorescence
			period = 60;
			width = 8;
			Settings.mappingDistance = 4;
			Settings.ScrollSpeed = 0.08f;

			background = new ColorMixer();
			stripe = new ColorMixer();
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
			// Gold, bright lamp
			period = 60;
			width = 10;
			Settings.mappingDistance = 4;
			Settings.ScrollSpeed = 0.05f;

			background = new ColorMixer();
			stripe = new ColorMixer();
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
			// Grey stone, lava band
			period = 60;
			width = 10;
			Settings.mappingDistance = 4;
			Settings.ScrollSpeed = 0.05f;

			background = new ColorMixer();
			stripe = new ColorMixer();
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

		case 5:
			// White plaster, black porcelain
			background = new ColorMixer();
			stripe = new ColorMixer();
			period = 60;
			width = 30;
			Settings.mappingDistance = 4;
			Settings.ScrollSpeed = 0.05f;
			
			background.InsertColorNode(Color.white, 0f);
			stripe.InsertColorNode(Color.black, 0f);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.mainTexture = tex;

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0.2f, 0.2f, 0.2f, 0.5f), 0);
			stripe.InsertColorNode(new Color(0.3f, 0.3f, 0.3f, 0.85f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.SetTexture("_SpecGlossMap", tex);
			mat.EnableKeyword("_SPECGLOSSMAP");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.1f, 1f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.1f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.9f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.1f, 0f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");

			mat.DisableKeyword("_EMISSION");
			break;
		}

	}

}
