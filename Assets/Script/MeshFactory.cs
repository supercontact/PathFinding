using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

public class ColorMixer {
	private struct ColorNode {
		public Color color;
		public float coeff;
		public ColorNode(Color color, float coeff) {
			this.color = color;
			this.coeff = coeff;
		}
	}
	private LinkedList<ColorNode> colors;
	public ColorMixer() {
		colors = new LinkedList<ColorNode>();
	}
	public void InsertColorNode(Color color, float coeff) {
		//LinkedListNode<ColorNode> n = new LinkedListNode<ColorNode>();
		LinkedListNode<ColorNode> current = colors.First;
		while (current != null && current.Value.coeff < coeff) {
			current = current.Next;
		}
		if (current == null)
			colors.AddLast(new ColorNode(color, coeff));
		else
			colors.AddBefore(current, new ColorNode(color, coeff));
	}
	public Color GetColor(float value) {
		LinkedListNode<ColorNode> current = colors.First;
		while (current != null && current.Value.coeff < value) {
			current = current.Next;
		}
		if (current == null) 
			return colors.Last.Value.color;
		if (current.Previous == null)
			return colors.First.Value.color;
		ColorNode left = current.Previous.Value;
		ColorNode right = current.Value;
		return Color.Lerp(left.color, right.color, (value - left.coeff) / (right.coeff - left.coeff));
	}
}

public class MeshFactory {

	public static Mesh ReadMeshFromFile(string file, float scaleFactor = 1, Vector3 offset = default(Vector3), Quaternion rot = default(Quaternion)) {
		int state = 0;
		int v = 0, f = 0, counter = 0;
		Vector3[] vertices = null;
		bool[] verticesIsUsed = null;
		int[] triangles = null;
		if (rot == default(Quaternion)) {
			rot = Quaternion.identity;
		}
		try
		{
			string line;
			StreamReader theReader = new StreamReader(file, Encoding.Default);
			using (theReader)
			{
				line = theReader.ReadLine();
				if (!line.Equals("OFF")) {
					Debug.Log("Not OFF!");
					return null;
				}
				do
				{
					line = theReader.ReadLine();
					if (line != null)
					{
						string[] entriesRaw = line.Split(' ');
						List<string> entries = new List<string>();
						for (int i = 0; i < entriesRaw.Length; i++) {
							if (!entriesRaw[i].Equals("")) {
								entries.Add(entriesRaw[i]);
							}
						}
						if (entries.Count > 0) {
							if (entries[0].Equals("#")) continue;
							if (state == 0) {
								v = int.Parse(entries[0]);
								f = int.Parse(entries[1]);
								vertices = new Vector3[v];
								verticesIsUsed = new bool[v];
								triangles = new int[3*f];
								state = 1;
								counter = 0;
							} else if (state == 1) {
								vertices[counter++] = rot * new Vector3(float.Parse(entries[0]), float.Parse(entries[1]), float.Parse(entries[2])) * scaleFactor + offset;
								if (counter == v) {
									state = 2;
									counter = 0;
								}
							} else if (state == 2) {
								if (!entries[0].Equals("3")) {
									Debug.Log(entries[0]+"Not a triangle mesh!");
									return null;
								}
								triangles[counter] = int.Parse(entries[1]);
								verticesIsUsed[triangles[counter++]] = true;
								triangles[counter] = int.Parse(entries[2]);
								verticesIsUsed[triangles[counter++]] = true;
								triangles[counter] = int.Parse(entries[3]);
								verticesIsUsed[triangles[counter++]] = true;
							}
						}
					}
				}
				while (line != null);
				theReader.Close();
			}
		}
		catch (Exception e)
		{
			Debug.Log(e.StackTrace);
			return null;
		}

		int[] verticesDeletedBefore = new int[v];
		verticesDeletedBefore[0] = verticesIsUsed[0] ? 0 : 1;
		for (int i = 1; i < v; i++) {
			verticesDeletedBefore[i] = verticesIsUsed[i] ? verticesDeletedBefore[i-1] : verticesDeletedBefore[i-1] + 1;
		}
		Vector3[] usedVertices = new Vector3[v - verticesDeletedBefore[v - 1]];
		for (int i = 0; i < v; i++) {
			if (verticesIsUsed[i]) {
				usedVertices[i - verticesDeletedBefore[i]] = vertices[i];
			}
		}
		for (int i = 0; i < 3 * f; i++) {
			triangles[i] -= verticesDeletedBefore[triangles[i]];
		}

		Mesh m = new Mesh();
		Debug.Log("New mesh loaded: "+file+" \nVertex count = "+usedVertices.Length+" triangle count = "+triangles.Length);
		m.vertices = usedVertices;
		m.triangles = triangles;
		m.RecalculateNormals();
		return m;
	}

	public static Mesh CreateSphere(float radius, int c) {
		Vector3[] vertices = new Vector3[2+(c-1)*2*c];
		vertices[0] = new Vector3(0, 0, radius);
		vertices[vertices.Length - 1] = new Vector3(0, 0, -radius);
		for (int i = 1; i < c; i++) {
			for (int j = 0; j < 2*c; j++) {
				vertices[1+(i-1)*2*c+j] = new Vector3(
					radius * Mathf.Sin(Mathf.PI*i/(float)c) * Mathf.Sin(0.5f * Mathf.PI*(2*j-i)/(float)c),
					radius * Mathf.Sin(Mathf.PI*i/(float)c) * Mathf.Cos(0.5f * Mathf.PI*(2*j-i)/(float)c),
					radius * Mathf.Cos(Mathf.PI*i/(float)c));
			}
		}
		int[] triangles = new int[12*c*(c-1)];
		int count = 0;
		for (int j = 0; j < 2*c; j++) {
			triangles[count++] = 0;
			triangles[count++] = 1 + (j+1)%(2*c);
			triangles[count++] = 1 + j;
		}
		for (int i = 1; i < c-1; i++) {
			for (int j = 0; j < 2*c; j++) {
				triangles[count++] = 1+(i-1)*2*c+j;
				triangles[count++] = 1+i*2*c+(j+1)%(2*c);
				triangles[count++] = 1+i*2*c+j;
				triangles[count++] = 1+(i-1)*2*c+j;
				triangles[count++] = 1+(i-1)*2*c+(j+1)%(2*c);
				triangles[count++] = 1+i*2*c+(j+1)%(2*c);
			}
		}
		for (int j = 0; j < 2*c; j++) {
			triangles[count++] = vertices.Length -1;
			triangles[count++] = 1+(c-2)*2*c+j;
			triangles[count++] = 1+(c-2)*2*c+(j+1)%(2*c);
		}
		Mesh m = new Mesh();
		m.vertices = vertices;
		m.triangles = triangles;
		m.RecalculateNormals();
		return m;
	}

	public static Texture2D CreateStripedTexture(int size, int stripePeriod, int stripeWidth, int offset, ColorMixer background, ColorMixer stripe, bool colorEveryPeriod = false) {
		Texture2D result = new Texture2D(size, 1, TextureFormat.ARGB32, false);
		for (int i = 0; i < size; i++) {
			int t = (i - offset) % stripePeriod;
			if (t < 0) {
				t += stripePeriod;
			}
			if (!colorEveryPeriod) {
				result.SetPixel(i, 0, t < stripeWidth ? stripe.GetColor(i/(float)size) : background.GetColor(i/(float)size));
			} else {
				result.SetPixel(i, 0, t < stripeWidth ? stripe.GetColor(t/(float)stripeWidth) : background.GetColor((t-stripeWidth)/(float)(stripePeriod-stripeWidth)));
			}
		}
		result.Apply();
		return result;
	}
}
