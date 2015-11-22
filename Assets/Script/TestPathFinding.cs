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
	public Mesh sphere;
	Mesh sphere2, sphere3;
	public Mesh dragon;
	public GameObject roadBase;
	public Text triangleCounter;
	public Text visualModeText;
	public InputField inputT;
	public GameObject visual;
	public GameObject gradArrow;

	Geometry g;
	HeatGeodesics hg;

	bool firstClick = true;
	bool useDefaultSettings = true;
	int levelIndex;
	int textureIndex;
	float t;
	float offset = 0;
	int visualState = 0;
	GameObject[] arrows;

	// Start is called at the beginng
	void Start () {
		sphere = Instantiate<Mesh>(sphere);
		sphere2 = Instantiate<Mesh>(sphere);
		sphere3 = MeshFactory.CreateSphere(1, 48);
		MeshFactory.MergeOverlappingPoints(sphere2);

		road = Instantiate<Mesh>(road);
		MeshFactory.TransformMesh(road, Vector3.zero, Quaternion.Euler(-180, 0, 0));

		dragon = Instantiate<Mesh>(dragon);
		MeshFactory.MergeOverlappingPoints(dragon);
		MeshFactory.TransformMesh(dragon, Vector3.zero, Quaternion.Euler(0, 90, 0), new Vector3(0.75f, 0.75f, 0.75f));


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

					UpdateVisualGradient();
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
		} else if (textureIndex == 6) {
			float tPeriod = 4f;
			float tLight = 1f;
			float c;
			t = (t + Time.deltaTime) % tPeriod;
			float tt = t % tLight;
			if (t / tLight < 3) {
				float cos = Mathf.Cos(Mathf.PI * (tt - tLight / 2) / tLight);
				c = 0.9f * cos;
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			} else {
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
		levelIndex = level;

		double tFactor = 1;
		int source = 42;
		int manPos = 42;
		Settings.cotLimit = 10000;

		switch (level) {
		case 0:
		default:
			earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/high_genus", 0.2f, new Vector3(0, 0, -0.6f));
			SetTexture(2);
			source = 94;
			manPos = 1947;
			break;
		case 1:
			earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/bague", 0.5f);
			SetTexture(0);
			source = 175;
			manPos = 230;
			break;
		case 2:
			earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/cow", 1.5f, new Vector3(0.15f, 0.15f, 0));
			SetTexture(3);
			source = 429;
			manPos = 2129;
			break;
		case 3:
		case 6:
			earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/tri_triceratops", 0.2f, new Vector3(0.15f, 0, 0));
			SetTexture(4);
			Settings.cotLimit = 5;
			source = 42;
			manPos = 918;
			break;
		case 4:
			//earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/bun_zipper.off", 12f, new Vector3(0.2f, -1f, 0));
			earth.GetComponent<MeshFilter>().mesh = MeshFactory.ReadMeshFromFile("OFF/horse1", 12f, new Vector3(0f, 0f, -0.3f), Quaternion.Euler(-90, 0, 0));
			SetTexture(5);
			tFactor = 10;
			source = 1559;
			manPos = 31107;
			break;
		case 5:
			earth.GetComponent<MeshFilter>().mesh = road;
			SetTexture(1);
			tFactor = 20;
			source = 285;
			manPos = 883;
			break;
		case 7:
			earth.GetComponent<MeshFilter>().mesh = sphere3;
			SetTexture(0);
			tFactor = 1;
			source = 3341;
			manPos = 7073;
			break;
		case 8:
			earth.GetComponent<MeshFilter>().mesh = sphere;
			SetTexture(0);
			tFactor = 1;
			source = 42;
			manPos = 775;
			break;
		case 9:
			earth.GetComponent<MeshFilter>().mesh = sphere2;
			SetTexture(0);
			tFactor = 1;
			source = 42;
			manPos = 775;
			break;
		case 10:
			earth.GetComponent<MeshFilter>().mesh = dragon;
			SetTexture(6);
			source = 42;
			manPos = 7284;
			break;
		}
		roadBase.SetActive(level == 5);

		if (useDefaultSettings) {
			Settings.tFactor = tFactor;
			Settings.defaultSource = source;
			Settings.initialManPos = manPos;
			inputT.text = Settings.tFactor.ToString();
			ClearVisualGradient();
		} else {
			Settings.defaultSource = hg.s.index;
			Settings.initialManPos = man.triangle.index;
		}

		// Set collider to detect mouse hit
		earth.GetComponent<MeshCollider>().sharedMesh = earth.GetComponent<MeshFilter>().mesh;

		// Build geometry data
		g = new Geometry(GetComponent<MeshFilter>().mesh);

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

		if (!useDefaultSettings) {
			UpdateVisualGradient();
		}
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
			stripe.InsertColorNode(new Color(1f,1f,0.5f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.2f, 1f), 0);
			//stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.5f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.2f, 0f), 1);
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

		case 6:
			// Yellow-red-black rubber,
			period = 82;
			width = 12;
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
			stripe.InsertColorNode(Color.yellow, 0.1f);
			stripe.InsertColorNode(Color.red, 0.4f);
			stripe.InsertColorNode(Color.black, 0.7f);
			stripe.InsertColorNode(Color.black, 0.8f);
			stripe.InsertColorNode(Color.red, 0.9f);
			stripe.InsertColorNode(Color.yellow, 1f);
			//stripe.InsertColorNode(new Color(1f,1f,0.5f), 0);
			/*stripe.InsertColorNode(new Color(1,1,0.5f), 0.1f);
			stripe.InsertColorNode(Color.yellow, 0.2f);
			stripe.InsertColorNode(Color.red, 0.5f);
			stripe.InsertColorNode(Color.black, 0.75f);
			stripe.InsertColorNode(Color.red, 0.85f);
			stripe.InsertColorNode(Color.yellow, 0.95f);
			stripe.InsertColorNode(new Color(1,1,0.5f), 1f);*/
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.mainTexture = tex;

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(Color.black, 0f);
			stripe.InsertColorNode(Color.yellow, 0.1f);
			stripe.InsertColorNode(Color.red, 0.4f);
			stripe.InsertColorNode(Color.black, 0.7f);
			stripe.InsertColorNode(Color.black, 0.8f);
			stripe.InsertColorNode(Color.red, 0.9f);
			stripe.InsertColorNode(Color.yellow, 1f);
			//stripe.InsertColorNode(new Color(1f,1f,0.5f), 0);
			/*stripe.InsertColorNode(new Color(1,1,0.5f), 0.1f);
			stripe.InsertColorNode(Color.yellow, 0.2f);
			stripe.InsertColorNode(Color.red, 0.5f);
			stripe.InsertColorNode(Color.black, 0.75f);
			stripe.InsertColorNode(Color.red, 0.85f);
			stripe.InsertColorNode(Color.yellow, 0.95f);
			stripe.InsertColorNode(new Color(1,1,0.5f), 1f);*/
			tex = MeshFactory.CreateStripedTexture(2048, period, width-2, 21, background, stripe);
			mat.SetTexture("_EmissionMap", tex);
			mat.SetColor("_EmissionColor", new Color(0.5f, 0.5f, 0.5f));
			mat.EnableKeyword("_EMISSION");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.1f, 0f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.2f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.8f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.1f, 1f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");

			//mat.DisableKeyword("_EMISSION");
			mat.DisableKeyword("_SPECGLOSSMAP");
			break;
		}

	}

	public void SetTFactor(string t) {
		if (double.TryParse(t, out Settings.tFactor)) {
			inputT.text = Settings.tFactor.ToString();
			useDefaultSettings = false;
			SetLevel(levelIndex);
			useDefaultSettings = true;
		} else {
			inputT.text = "invalid";
		}
	}

	public void VisualizeGradient(bool heat) {
		ClearVisualGradient();
		arrows = new GameObject[g.faces.Count];
		for (int i = 0; i < g.faces.Count; i++) {
			arrows[i] = GameObject.Instantiate(gradArrow);
		}
		visualState = heat ? 2 : 1;
		UpdateVisualGradient();
		visualModeText.text = "Visualization Mode = " + (heat ? "Heat gradient" : "Distance gradient");
	}

	public void UpdateVisualGradient() {
		if (visualState == 2) {
			for (int i = 0; i < g.faces.Count; i++) {
				Face face = g.faces[i];
				arrows[i].transform.SetParent(visual.transform);
				arrows[i].transform.position = face.CalculateCenter();
				arrows[i].transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
				if (hg.X[i] != Vector3.zero) {
					arrows[i].transform.rotation = Quaternion.LookRotation(hg.X[i], face.CalculateNormalTri());
				} else {
					arrows[i].transform.rotation = Quaternion.LookRotation(face.CalculateNormalTri());
				}
			}
			visualModeText.text = "Visualization Mode = Heat gradient";
		} else if (visualState == 1) {
			for (int i = 0; i < g.faces.Count; i++) {
				Face face = g.faces[i];
				arrows[i].transform.SetParent(visual.transform);
				arrows[i].transform.position = face.CalculateCenter();
				arrows[i].transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
				if (hg.GradPhi[i] != Vector3.zero) {
					arrows[i].transform.rotation = Quaternion.LookRotation(-hg.GradPhi[i], face.CalculateNormalTri());
				} else {
					arrows[i].transform.rotation = Quaternion.LookRotation(face.CalculateNormalTri());
				}
			}
			visualModeText.text = "Visualization Mode = Distance gradient";
		}
	}

	public void ClearVisualGradient() {
		for (int i = 0; i < visual.transform.childCount; i++) {
			Destroy(visual.transform.GetChild(i).gameObject);
		}
		arrows = null;
		visualState = 0;
		visualModeText.text = "";
	}

	public void ToggleGradient() {
		if (visualState == 0) {
			VisualizeGradient(false);
		} else if (visualState == 1) {
			visualState = 2;
			UpdateVisualGradient();
		} else {
			ClearVisualGradient();
		}
	}
}
