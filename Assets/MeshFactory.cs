using UnityEngine;
using System.Collections;
using System.Text;
using System.IO;
using System;

public class MeshFactory {

	public static Mesh ReadMeshFromFile(string file) {
		int state = 0;
		int v = 0, f = 0, counter = 0;
		Vector3[] vertices = null;
		int[] triangles = null;
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
						string[] entries = line.Split(' ');
						if (entries.Length > 0) {
							if (entries[0].Equals("#") || entries[0].Equals("")) continue;
							if (state == 0) {
								v = int.Parse(entries[0]);
								f = int.Parse(entries[1]);
								vertices = new Vector3[v];
								triangles = new int[3*f];
								state = 1;
								counter = 0;
							} else if (state == 1) {
								vertices[counter++] = new Vector3(float.Parse(entries[0]), float.Parse(entries[1]), float.Parse(entries[2])) / 5;
								if (counter == v) {
									state = 2;
									counter = 0;
								}
							} else if (state == 2) {
								if (!entries[0].Equals("3")) {
									Debug.Log(entries[0]+"Not a triangle mesh!");
									return null;
								}
								triangles[counter++] = int.Parse(entries[1]);
								triangles[counter++] = int.Parse(entries[2]);
								triangles[counter++] = int.Parse(entries[3]);
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
			//Console.WriteLine("{0}\n", e.Message);
			Debug.Log(e.Message);
			return null;
		}
		Mesh m = new Mesh();
		Debug.Log("New mesh loaded: "+file+" \nVertex count = "+vertices.Length+" triangle count = "+triangles.Length);
		m.vertices = vertices;
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

	public static Texture2D CreateStripedTexture() {
		return null;
	}
}
