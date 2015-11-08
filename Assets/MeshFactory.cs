using UnityEngine;
using System.Collections;

public class MeshFactory {

	public static Mesh CreateSphere(float radius, int c) {
		Vector3[] vertices = new Vector3[2+(c-1)*2*c];
		vertices[0] = new Vector3(0, 0, radius);
		vertices[vertices.Length - 1] = new Vector3(0, 0, -radius);
		for (int i = 1; i < c; i++) {
			for (int j = 0; j < 2*c; j++) {
				vertices[1+(i-1)*2*c+j] = new Vector3(
					radius * Mathf.Sin(Mathf.PI*i/(float)c) * Mathf.Sin(Mathf.PI*j/(float)c),
					radius * Mathf.Sin(Mathf.PI*i/(float)c) * Mathf.Cos(Mathf.PI*j/(float)c),
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
}
