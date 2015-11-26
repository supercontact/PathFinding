using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The main program.
/// </summary>
public class TestPathFinding : MonoBehaviour {

	// Public variables that are linked via Unity inspector
	public GameObject earth;
	public Camera cam;
	public WalkingMan man;
	public Mesh road;
	public Mesh sphere;
	Mesh sphere2; // Copy of sphere + processing
	Mesh sphere3; // Created via script
	public Mesh hemisphere;
	public Mesh skull;
	public Mesh dragon;
	Mesh highG, bague, cow, triceratops, horse; // Loaded OFF files
	public Text triangleCounter;
	public Text visualModeText;
	public Text heatInfo;
	public Text warning;
	public InputField inputT;
	public Image background;
	public GameObject pin;
	public GameObject roadBase;
	public GameObject hemiCap;
	public GameObject skullBase;
	public GameObject visual;
	public GameObject gradArrow;
	public GameObject cursor;
	public Material wireframe;
	public LineRenderer line;

	Geometry g; // The main geometry (mesh)
	HeatGeodesics hg; // The heat geodesics calculation module
	
	bool clicking = false;
	bool firstClick = true;
	bool useDefaultSettings = true;
	int levelIndex;
	int textureIndex;
	float t;
	float offset = 0;
	int visualState = 0;
	GameObject[] arrows;
	List<GameObject> pins;
	Material mainMat;
	int materialMode = 0;
	float changedBoundaryCond = -1f;
	Vertex lastClickedVertex;

	ColorMixer anim1;

	// Start is called at the beginng
	void Start () {
		mainMat = earth.GetComponent<MeshRenderer>().material;
		pins = new List<GameObject>();

		// Initializing meshes
		highG = MeshFactory.ReadMeshFromFile("OFF/high_genus", 0.2f, new Vector3(0, 0, -0.6f));
		MeshFactory.ReorderVertexIndices(highG, 0);

		bague = MeshFactory.ReadMeshFromFile("OFF/bague", 0.5f);
		MeshFactory.ReorderVertexIndices(bague, 0);

		cow = MeshFactory.ReadMeshFromFile("OFF/cow", 1.5f, new Vector3(0.15f, 0.15f, 0));

		triceratops = MeshFactory.ReadMeshFromFile("OFF/tri_triceratops", 0.2f, new Vector3(0.15f, 0, 0));

		horse = MeshFactory.ReadMeshFromFile("OFF/horse1", 12f, new Vector3(0f, 0f, -0.3f), Quaternion.Euler(-90, 0, 0));
		MeshFactory.ReorderVertexIndices(horse, 2);

		sphere = Instantiate<Mesh>(sphere);
		sphere2 = Instantiate<Mesh>(sphere);
		sphere3 = MeshFactory.CreateSphere(1, 48);
		MeshFactory.MergeOverlappingPoints(sphere2);

		hemisphere = Instantiate<Mesh>(hemisphere);
		MeshFactory.MergeOverlappingPoints(hemisphere);
		MeshFactory.TransformMesh(hemisphere, Vector3.zero, Quaternion.Euler(-90, 0, 0));

		skull = Instantiate<Mesh>(skull);
		MeshFactory.MergeOverlappingPoints(skull);
		MeshFactory.TransformMesh(skull, new Vector3(0, -0.5f, 0), Quaternion.Euler(-90, 0, 0));
		MeshFactory.ReorderVertexIndices(skull, 1);

		road = Instantiate<Mesh>(road);
		MeshFactory.TransformMesh(road, Vector3.zero, Quaternion.Euler(-180, 0, 0));

		dragon = Instantiate<Mesh>(dragon);
		MeshFactory.MergeOverlappingPoints(dragon);
		MeshFactory.TransformMesh(dragon, Vector3.zero, Quaternion.Euler(0, 90, 0), new Vector3(0.75f, 0.75f, 0.75f));
		MeshFactory.ReorderVertexIndices(dragon, 0);

		// Emission animation
		float tTotal = 6f, tBlink = 0.10f;
		float cMax = 0.9f, cM1 = 0.3f, cM2 = 0.5f;
		anim1 = new ColorMixer();
		anim1.InsertColorNode(new Color(cMax, cMax, cMax), 0f);
		anim1.InsertColorNode(new Color(cMax, cMax, cMax), 0.5f - 3f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(cM1, cM1, cM1), 0.5f - 2.5f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(cMax, cMax, cMax), 0.5f - 2f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(cM1, cM1, cM1), 0.5f - 1.5f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(cMax, cMax, cMax), 0.5f - 1f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(0f, 0f, 0f), 0.5f);
		anim1.InsertColorNode(new Color(0f, 0f, 0f), 1f - 3f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(cM2, cM2, cM2), 1f - 2.5f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(0f, 0f, 0f), 1f - 2f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(cM2, cM2, cM2), 1f - 1.5f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(0f, 0f, 0f), 1f - 1f * tBlink / tTotal);
		anim1.InsertColorNode(new Color(cMax, cMax, cMax), 1f);

		// Start with the first mesh
		SetLevel(0);
		UpdateInfoText();
	}


	// Update is called once per frame
	void Update () {
		line.enabled = false;

		// Find the triangle and the closest vertex the mouse is pointing to
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		RaycastHit info;
		bool pressingAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

		cursor.transform.position = new Vector3(1000, 0, 0);
		heatInfo.rectTransform.position = new Vector3(10000, 0, 0);

		if ((clicking || pressingAlt) && Physics.Raycast(ray, out info)) {
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

				if (pressingAlt) {
					// Show vertex info
					cursor.transform.position = v.p;
					Vector3 vertScreenPoint = cam.WorldToScreenPoint(v.p);
					heatInfo.rectTransform.position = new Vector3(Mathf.Round(vertScreenPoint.x) + 170, Mathf.Round(vertScreenPoint.y) - 32, 0);
					heatInfo.text = "Vertex distance = " + hg.phi[v.index];
					heatInfo.text += "\nVertex heat = " + hg.u[v.index];
					heatInfo.text += "\nVertex index = " + v.index;
					if (v.onBorder) {
						heatInfo.text += "\n<color=#ff2200>Boundary vertex</color>";
					}

					// Trace the shortest path
					line.enabled = true;
					SurfaceObject obj = new SurfaceObject(g, hg, f, info.point);
					List<Vector3> traj = obj.GoForward(500, 500, 0.001f);
					line.SetVertexCount(traj.Count);
					int i = 0;
					foreach (Vector3 pt in traj) {
						line.SetPosition(i, pt);
						i++;
					}
				}

				// When clicking on the surface of the mesh, reset source
				if (clicking) {
					Debug.Log("triangle hit = " + info.triangleIndex);
					Debug.Log("vertex hit = " + v.index);

					// Set the source
					bool wholeLine = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
					bool additional = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || wholeLine;
					List<Vertex> lineSources = null;

					if (!wholeLine) {
						hg.CalculateGeodesics(v, additional);
					} else {
						DijkstraEdgePathFinding deef = new DijkstraEdgePathFinding(g, lastClickedVertex);
						lineSources = deef.GetPathFrom(v);
						hg.CalculateGeodesics(lineSources, true);
					}
					warning.gameObject.SetActive(additional && Settings.useCholesky);

					// Put the mark at the source vertex
					if (!additional) {
						foreach (GameObject oldPin in pins) {
							Destroy(oldPin);
						}
						pins.Clear();
					}
					if (!wholeLine) {
						GameObject newPin = Instantiate(pin);
						pins.Add(newPin);
						newPin.transform.position = v.p;
						newPin.transform.rotation = Quaternion.LookRotation(v.CalculateNormalTri());
					} else {
						foreach (Vertex vert in lineSources) {
							GameObject newPin = Instantiate(pin);
							pins.Add(newPin);
							newPin.transform.position = vert.p;
							newPin.transform.rotation = Quaternion.LookRotation(vert.CalculateNormalTri());
						}
					}

					lastClickedVertex = v;

					// Hide tutorial text
					if (firstClick) {
						GameObject.Find("Tutorial").SetActive(false);
						firstClick = false;
					}

					UpdateVisualGradient();
				}
			}
		}
		clicking = false;

		// User changed the boundary condition through the slider
		if (Input.GetMouseButtonUp(0) && changedBoundaryCond >= 0) {
			Settings.boundaryCondition = changedBoundaryCond;
			useDefaultSettings = false;
			SetLevel(levelIndex);
			useDefaultSettings = true;
			changedBoundaryCond = -1;
		}

		// Scrolling the surface texture
		offset -= Settings.ScrollSpeed * Time.deltaTime;
		mainMat.mainTextureOffset = new Vector2(offset, 0);

		// Emission texture animation
		Material mat = mainMat;
		if (textureIndex == 2) {
			float T = 6f;
			t = (t + Time.deltaTime) % T;
			mat.SetColor("_EmissionColor", anim1.GetColor(t / T));
			Settings.ScrollSpeed = (t < T / 2 || t > T - 0.1f) ? 0.08f : 0.04f;

		} else if (textureIndex == 3) {
			float tPeriod = 7f, tLight = 3f;
			t = (t + Time.deltaTime) % tPeriod;
			if (t < tLight) {
				float cos = Mathf.Cos(Mathf.PI * (t - tLight / 2) / tLight);
				float c = 0.7f * cos;
				Settings.ScrollSpeed = 0.05f * (1 - 0.9f * cos);
				mat.SetColor("_EmissionColor", new Color(c, c, c));
			} else  {
				mat.SetColor("_EmissionColor", new Color(0f, 0f, 0f));
			}

		} else if (textureIndex == 6) {
			float tPeriod = 4f, tLight = 1f;
			t = (t + Time.deltaTime) % tPeriod;
			float tt = t % tLight;
			if (t / tLight < 3) {
				float cos = Mathf.Cos(Mathf.PI * (tt - tLight / 2) / tLight);
				float c = 0.9f * cos;
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
			earth.GetComponent<MeshFilter>().mesh = highG;
			SetTexture(2);
			source = 1537;
			manPos = 1947;
			break;
		case 1:
			earth.GetComponent<MeshFilter>().mesh = bague;
			SetTexture(0);
			source = 280;
			manPos = 230;
			break;
		case 2:
			earth.GetComponent<MeshFilter>().mesh = cow;
			SetTexture(3);
			source = 429;
			manPos = 2129;
			break;
		case 3:
		case 6:
			earth.GetComponent<MeshFilter>().mesh = triceratops;
			SetTexture(4);
			Settings.cotLimit = 5;
			source = 42;
			manPos = 918;
			break;
		case 4:
			earth.GetComponent<MeshFilter>().mesh = horse;
			SetTexture(5);
			tFactor = 10;
			source = 435;
			manPos = 31107;
			break;
		case 5:
			earth.GetComponent<MeshFilter>().mesh = road;
			SetTexture(1);
			source = 285;
			manPos = 883;
			break;
		case 7:
			earth.GetComponent<MeshFilter>().mesh = sphere3;
			SetTexture(7);
			source = 3341;
			manPos = 7073;
			break;
		case 8:
			earth.GetComponent<MeshFilter>().mesh = sphere;
			SetTexture(7);
			source = 42;
			manPos = 775;
			break;
		case 9:
			earth.GetComponent<MeshFilter>().mesh = sphere2;
			SetTexture(7);
			source = 42;
			manPos = 775;
			break;
		case 11:
			earth.GetComponent<MeshFilter>().mesh = hemisphere;
			SetTexture(7);
			source = 1331;
			manPos = 475;
			break;
		case 12:
			earth.GetComponent<MeshFilter>().mesh = skull;
			SetTexture(8);
			source = 5794;
			manPos = 781;
			break;
		case 10:
			earth.GetComponent<MeshFilter>().mesh = dragon;
			SetTexture(6);
			source = 7561;
			manPos = 7284;
			break;
		}
		roadBase.SetActive(level == 5);
		hemiCap.SetActive(level == 11);
		skullBase.SetActive(level == 12);

		// Adjust settings
		if (useDefaultSettings) {
			Settings.tFactor = tFactor;
			Settings.defaultSource = new List<int>();
			Settings.defaultSource.Add(source);
			Settings.initialManPos = manPos;
			inputT.text = Settings.tFactor.ToString();
		} else {
			Settings.defaultSource = new List<int>();
			foreach (Vertex sourceVertex in hg.s) {
				Settings.defaultSource.Add(sourceVertex.index);
			}
			Settings.initialManPos = man.coord.triangle.index;
		}

		// Set collider to detect mouse hit
		earth.GetComponent<MeshCollider>().sharedMesh = earth.GetComponent<MeshFilter>().mesh;

		// Build geometry data
		g = new Geometry(earth.GetComponent<MeshFilter>().mesh);

		if (level == 6) {
			// Fix certain broken triangles for the triceratops
			g.FixVertex(758);
			g.FixVertex(295);
			g.FixVertex(395);
			g.FixVertex(2449);
		}

		// Start heat method
		hg = new HeatGeodesics(g, Settings.useCholesky);
		hg.Initialize();

		List<Vertex> sources = new List<Vertex>();
		foreach (int ind in Settings.defaultSource) {
			sources.Add(g.vertices[ind]);
			lastClickedVertex = g.vertices[ind];
		}
		hg.CalculateGeodesics(sources);

		// Spawn the little man
		if (man.isActiveAndEnabled) {
			man.GetReady(g, hg, g.faces[Settings.initialManPos]);
		}

		// Put the mark at the source vertices
		if (useDefaultSettings) {
			foreach (GameObject oldPin in pins) {
				Destroy(oldPin);
			}
			pins.Clear();
			foreach (int ind in Settings.defaultSource) {
				GameObject newPin = Instantiate(pin);
				pins.Add(newPin);
				newPin.transform.position = g.vertices[ind].p;
				newPin.transform.rotation = Quaternion.LookRotation(g.vertices[ind].CalculateNormalTri());
			}
		}

		// Update counter
		triangleCounter.text = "Triangle Count = " + g.faces.Count;

		if (visualState != 0) {
			if (useDefaultSettings) {
				VisualizeGradient(visualState == 2);
			} else {
				UpdateVisualGradient();
			}
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
		Material mat = mainMat;

		int period;
		int width;

		switch (type) {

		case 0:
		default:
			// White purple rubber, bright yellow gap
			period = 82;
			width = 11;
			Settings.mappingDistance = 3;
			Settings.ScrollSpeed = 0.066f;

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.9f, 1f), 0.1f);
			background.InsertColorNode(new Color(1f, 0, 1f), 0.4f);
			background.InsertColorNode(new Color(0.4f, 0, 0.6f), 0.7f);
			background.InsertColorNode(new Color(0.4f, 0, 0.6f), 0.8f);
			background.InsertColorNode(new Color(1f, 0, 1f), 0.9f);
			background.InsertColorNode(new Color(1f, 0.9f, 1f), 1f);
			stripe.InsertColorNode(new Color(0.9f,0.9f,0.45f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.2f, 1f), 0);
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
			// Yellow-red-black rubber + light
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
			tex = MeshFactory.CreateStripedTexture(2048, period, 0, 20, background, stripe);
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

			mat.DisableKeyword("_SPECGLOSSMAP");
			break;

		case 7:
			// White-yellow-orange plaster, black stripe
			period = 82;
			width = 11;
			Settings.mappingDistance = 3;
			Settings.ScrollSpeed = 0.066f;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 1f, 0.8f), 0.1f);
			background.InsertColorNode(new Color(1f, 1f, 0f), 0.3f);
			background.InsertColorNode(new Color(1f, 0.3f, 0f), 0.7f);
			background.InsertColorNode(new Color(1f, 0.3f, 0f), 0.8f);
			background.InsertColorNode(new Color(1f, 1f, 0f), 0.93f);
			background.InsertColorNode(new Color(1f, 1f, 0.8f), 1f);
			stripe.InsertColorNode(new Color(0.1f, 0.1f, 0.1f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.mainTexture = tex;

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0.33f, 0.33f, 0.33f, 0.33f), 0);
			stripe.InsertColorNode(new Color(0.4f, 0.4f, 0.4f, 0.5f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.SetTexture("_SpecGlossMap", tex);
			mat.EnableKeyword("_SPECGLOSSMAP");

			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.2f, 1f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.2f, 0f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");
			
			mat.DisableKeyword("_EMISSION");
			break;

		case 8:
			// White bone, metal band
			period = 82;
			width = 22;
			Settings.mappingDistance = 3;
			Settings.ScrollSpeed = 0.066f;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0.6f, 0.6f, 0.6f), 0f);
			stripe.InsertColorNode(new Color(0f, 0f, 0f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.mainTexture = tex;
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(0f, 0f, 0f, 0.4f), 0);
			stripe.InsertColorNode(new Color(0.7f, 0.7f, 0.7f, 0.8f), 0);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe);
			mat.SetTexture("_SpecGlossMap", tex);
			mat.EnableKeyword("_SPECGLOSSMAP");
			
			background = new ColorMixer();
			stripe = new ColorMixer();
			background.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.2f, 0f), 0);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.1f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.5f, 0.5f), 0.9f);
			stripe.InsertColorNode(new Color(1f, 0.5f, 0.2f, 1f), 1);
			tex = MeshFactory.CreateStripedTexture(2048, period, width, 20, background, stripe, true);
			mat.SetTexture("_BumpMap", tex);
			mat.EnableKeyword("_NORMALMAP");
			
			mat.DisableKeyword("_EMISSION");
			break;
		}

	}

	/// <summary>
	/// This function is executed when the user changes the value of t using the input field.
	/// </summary>
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

	/// <summary>
	/// Actives the gradient visualization.
	/// If heat is set to true, the gradient of the heat field will be drawn instead of the distance gradient.
	/// </summary>
	public void VisualizeGradient(bool heat) {
		ClearVisualGradient();
		arrows = new GameObject[g.faces.Count];
		for (int i = 0; i < g.faces.Count; i++) {
			arrows[i] = GameObject.Instantiate(gradArrow);
		}
		visualState = heat ? 2 : 1;
		UpdateVisualGradient();
		UpdateInfoText();
	}

	/// <summary>
	/// Updates the gradient visualization.
	/// </summary>
	public void UpdateVisualGradient() {
		if (visualState == 2) {
			// Heat gradient
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
			UpdateInfoText();
		} else if (visualState == 1) {
			// Distance gradient
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
			UpdateInfoText();
		}
	}

	/// <summary>
	/// Clears the gradient visualization.
	/// </summary>
	public void ClearVisualGradient() {
		for (int i = 0; i < visual.transform.childCount; i++) {
			Destroy(visual.transform.GetChild(i).gameObject);
		}
		arrows = null;
		visualState = 0;
		UpdateInfoText();
	}

	/// <summary>
	/// Toggles between distance gradient, heat gradient and nothing.
	/// </summary>
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

	/// <summary>
	/// Toggles between wireframe and normal material.
	/// </summary>
	public void ToggleMaterialMode() {
		if (materialMode == 0) {
			materialMode = 1;
			earth.GetComponent<MeshRenderer>().material = wireframe;
		} else {
			materialMode = 0;
			earth.GetComponent<MeshRenderer>().material = mainMat;
		}
		UpdateInfoText();
	}

	/// <summary>
	/// Toggles usage of Cholesky decomposition.
	/// </summary>
	public void ToggleCholesky(bool on) {
		Settings.useCholesky = on;
		useDefaultSettings = false;
		SetLevel(levelIndex);
		useDefaultSettings = true;
	}

	/// <summary>
	/// Change the boundary condition ratio
	/// </summary>
	public void ChangeBoundaryCondition(float coeff) {
		changedBoundaryCond = coeff;
	}

	public void Clicking(BaseEventData data) {
		PointerEventData pdata = (PointerEventData) data;
		clicking = pdata.button == PointerEventData.InputButton.Left;
	}

	private void UpdateInfoText() {
		if (visualState == 0) {
			visualModeText.text = "";
		} else if (visualState == 1) {
			visualModeText.text = "Visualization Mode = Distance gradient";
		} else if (visualState == 2) {
			visualModeText.text = "Visualization Mode = Heat gradient";
		}
		if (materialMode == 1) {
			if (visualState == 0) {
				visualModeText.text = "Visualization Mode = Wireframe";
			} else {
				visualModeText.text += " + Wireframe";
			}
		}
	}
}
