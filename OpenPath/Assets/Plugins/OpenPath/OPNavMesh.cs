using UnityEngine;
using System.Collections;

using System.Collections.Generic;

public class OPNavMesh : MonoBehaviour
{
	private class Triangle
	{
		public int[] indices;

		public Triangle(int v0, int v1, int v2)
		{
			indices = new int[3];
			indices[0] = v0;
			indices[1] = v1;
			indices[2] = v2;
		}

		private bool IsNeighborTo(Triangle t, Vector3[] vertices)
		{
			int similarVertices = 0;

			foreach (int thisVertex in indices)
			{
				foreach (int thatVertex in t.indices)
				{
					if (vertices[thisVertex] == vertices[thatVertex])
					{
						similarVertices++;
					}
				}
			}

			return similarVertices > 1;
		}

		public List<int> GetNeighbors(Triangle[] triangles, Vector3[] vertices)
		{
			List<int> tempList = new List<int>();

			for (int i = 0; i < triangles.Length; i++)
			{
				if (IsNeighborTo(triangles[i], vertices))
				{
					tempList.Add(i);
				}
			}

			return tempList;
		}

		public Vector3 GetMedianPoint(Mesh mesh)
		{
			Vector3 result = Vector3.zero;

			for (int i = 0; i < indices.Length; i++)
			{
				result += mesh.vertices[indices[i]];
			}

			result /= indices.Length;

			return result;
		}
	}

	/*void OnDrawGizmos()
	{
		Mesh mesh = this.GetComponent<MeshFilter>().sharedMesh;
		List<Triangle> triangleList = new List<Triangle>();
		List<OPNode> allNodes = new List<OPNode>();

		int i = 0;
		int nb = 0;

		// Create triangles
		for (i = 0; i < mesh.triangles.Length; i += 3)
		{
			Triangle triangle = new Triangle(
				mesh.triangles[i],
				mesh.triangles[i + 1],
				mesh.triangles[i + 2]
			);

			triangleList.Add(triangle);

			// Create median node
			OPNode mn = new OPNode();
			mn.position = this.transform.TransformPoint(triangle.GetMedianPoint(mesh));

			// Add median node to list
			allNodes.Add(mn);
		}

		Triangle[] triangleArray = triangleList.ToArray();
		Vector3[] vertices = mesh.vertices;

		foreach(var no in allNodes)
		{
			Gizmos.DrawCube(no.position, new Vector3(0.25f, 0.25f, 0.25f));
		}

		for (i = 0; i < triangleArray.Length; i++)
		{
			var n = triangleArray[i];

			Gizmos.color = Color.white;

			Gizmos.DrawLine(vertices[n.indices[0]], vertices[n.indices[1]]);
			Gizmos.DrawLine(vertices[n.indices[1]], vertices[n.indices[2]]);
			Gizmos.DrawLine(vertices[n.indices[2]], vertices[n.indices[0]]);

			Gizmos.color = Color.white;
		}
	}*/

	private void MakeNeighbors(OPNode a, OPNode b)
	{
		if (a == b)
			return;

		if (!a.neighbors.Contains(b))
			a.neighbors.Add(b);

		if (!b.neighbors.Contains(a))
		{
			b.neighbors.Add(a);
		}
	}

	public OPNode[] GetNodes()
	{
		Mesh mesh = this.GetComponent<MeshFilter>().sharedMesh;
		List<Triangle> triangleList = new List<Triangle>();
		List<OPNode> allNodes = new List<OPNode>();

		int i = 0;
		int nb = 0;

		// Create triangles
		for (i = 0; i < mesh.triangles.Length; i += 3)
		{
			Triangle triangle = new Triangle(
				mesh.triangles[i],
				mesh.triangles[i + 1],
				mesh.triangles[i + 2]
			);

			triangleList.Add(triangle);

			// Create median node
			OPNode mn = new OPNode();
			mn.position = this.transform.TransformPoint(triangle.GetMedianPoint(mesh));

			// Add median node to list
			allNodes.Add(mn);
		}

		Triangle[] triangleArray = triangleList.ToArray();
		Vector3[] vertices = mesh.vertices;

		// Connect median nodes
		for (i = 0; i < triangleArray.Length; i++)
		{
			var gn = triangleArray[i].GetNeighbors(triangleArray, vertices);
			for (nb = 0; nb < gn.Count; nb++)
			{
				MakeNeighbors(allNodes[i], allNodes[gn[nb]]);
			}
		}

		// Return
		return allNodes.ToArray();
	}
}
