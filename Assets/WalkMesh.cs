using UnityEngine;
using System.Collections;

public class WalkMesh : MonoBehaviour {

	public GameObject stick;
	Geometry g;
	Halfedge h;
	// Use this for initialization
	void Start () {
		g = new Geometry(GetComponent<MeshFilter>().mesh);
		h = g.halfedges[0];
		DrawEdge();
	}

	void DrawEdge() {
		stick.transform.position = (h.vertex.p + h.prev.vertex.p) / 2;
		float scale = (h.vertex.p - h.prev.vertex.p).magnitude / 2f;
		stick.transform.localScale = new Vector3(scale, scale, scale);
		stick.transform.rotation = Quaternion.LookRotation(h.vertex.p - h.prev.vertex.p);
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.UpArrow)) {
			h = h.next;
			DrawEdge();
		}
		if (Input.GetKeyDown(KeyCode.DownArrow)) {
			h = h.prev;
			DrawEdge();
		}
		if (Input.GetKeyDown(KeyCode.Space)) {
			h = h.opposite;
			DrawEdge();
		}
	}
}
