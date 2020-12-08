using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static OPNavMesh;

public enum OPMapType
{
	Grid,
	WayPoint,
	NavMesh
}

[System.Serializable]
public class OPMap
{
	[System.NonSerialized] public OPNode[] nodes = null;

	public OPNode GetNode(Vector3 position)
	{
		foreach (OPNode node in nodes)
		{
			if (node.position == position)
			{
				return node;
			}
		}

		return null;
	}

	public int GetIndex(OPNode node)
	{
		return System.Array.IndexOf(nodes, node);
	}

	public List<OPNode> GetNeighbors(OPNode node)
	{
		return node.neighbors;
	}

	public void Reset()
	{
		foreach (OPNode n in nodes)
		{
			if (n != null)
			{
				n.parent = null;
			}
		}
	}
}


////////////////////
// NavMesh
////////////////////
[System.Serializable]
public class OPNavMeshMap : OPMap
{
	public OPNavMeshMap(OPNavMesh navMesh) : this(new OPNavMesh[] { navMesh }) { }

	public OPNavMeshMap(OPNavMesh[] navMeshes)
	{
		if (navMeshes == null || navMeshes.Length < 1)
		{
			Debug.LogError("OPMap | No active NavMesh in scene!");
		}
		else
		{
			if (navMeshes.Length == 1)
				nodes = navMeshes[0].GetNodes();
			else
			{
				nodes = GetNodes(navMeshes);
				/*List<OPNode> nodeList = new List<OPNode>();

				foreach (var nm in navMeshes)
				{
					nodeList.AddRange(nm.GetNodes());
				}

				nodes = nodeList.ToArray();*/
			}
		}
	}

	public OPNode[] GetNodes(OPNavMesh[] navMeshes)
	{
		List<Triangle> combinedTriangles = new List<Triangle>();
		List<Vector3> combinedVertices = new List<Vector3>();
		List<OPNode> combinedNodes = new List<OPNode>();

		foreach(var navMesh in navMeshes)
		{
			Mesh mesh = navMesh.GetComponent<MeshFilter>().sharedMesh;

			// Create triangles
			for (int i = 0; i < mesh.triangles.Length; i += 3)
			{
				Triangle triangle = new Triangle(
					mesh.triangles[i],
					mesh.triangles[i + 1],
					mesh.triangles[i + 2]
				);

				combinedTriangles.Add(triangle);

				// Create median node
				OPNode mn = new OPNode();
				mn.position = navMesh.transform.TransformPoint(triangle.GetMedianPoint(mesh));

				// Add median node to list
				combinedNodes.Add(mn);
			}

			combinedVertices.AddRange(mesh.vertices);
		}

		// Connect median nodes
		for (int i = 0; i < combinedTriangles.Count; i++)
		{
			var gn = combinedTriangles[i].GetNeighbors(combinedTriangles, combinedVertices);
			for (int nb = 0; nb < gn.Count; nb++)
			{
				OPNode.MakeNeighbors(combinedNodes[i], combinedNodes[gn[nb]]);
			}
		}

		for (int i = 0; i < combinedNodes.Count; i++)
		{
			Debug.Log(combinedNodes[i].neighbors.Count);
		}


		// Return
		return combinedNodes.ToArray();
	}
}


//////////////////
// Waypoint
//////////////////
[System.Serializable]
public class OPWayPointMap : OPMap
{
	public OPWayPointMap(OPWayPoint[] nodeContainers)
	{
		List<OPNode> tempList = new List<OPNode>();

		foreach (OPWayPoint n in nodeContainers)
		{
			n.FindNeighbors(nodeContainers);

			tempList.Add(n.node);
		}

		nodes = tempList.ToArray();

		foreach (OPWayPoint n in GameObject.FindObjectsOfType(typeof(OPWayPoint)))
		{
			MonoBehaviour.Destroy(n.gameObject);
		}
	}
}


//////////////////
// Grid
//////////////////
[System.Serializable]
public class OPGridMap : OPMap
{
	float spacing;
	//private int count = 0;

	public OPGridMap(Vector3 start, Vector3 size, float gridSpacing, LayerMask layerMask)
	{
		List<OPNode> tempList = new List<OPNode>();

		spacing = gridSpacing;

		int x;
		int z;

		// Raycast from every point in a horizontal grid
		for (x = 0; x < size.x; x++)
		{
			for (z = 0; z < size.z; z++)
			{
				Vector3 from = new Vector3(start.x + (x * spacing), start.y + (size.y * spacing), start.z + (z * spacing));
				RaycastHit[] hits = RaycastContinuous(from, layerMask);

				// Add all hits to the list
				for (int r = 0; r < hits.Length; r++)
				{
					Vector3 p = hits[r].point;
					OPNode n = new OPNode(p.x, p.y, p.z);
					tempList.Add(n);
				}
			}
		}

		nodes = tempList.ToArray();

		FindNeighbors();
	}

	// Raycast continuously through several objects
	private RaycastHit[] RaycastContinuous(Vector3 from, LayerMask layerMask)
	{
		List<RaycastHit> hits = new List<RaycastHit>();
		RaycastHit currentHit = new RaycastHit();

		if (Physics.Raycast(from, Vector3.down, out currentHit, Mathf.Infinity, layerMask))
		{
			hits.Add(currentHit);

			// We're allowing maximum 10 consecutive hits to be detected
			for (int i = 0; i < 10; i++)
			{
				if (Physics.Raycast(currentHit.point + Vector3.down, Vector3.down, out currentHit, Mathf.Infinity, layerMask))
				{
					bool left = Physics.Raycast(currentHit.point, Vector3.left, spacing / 2, layerMask);
					bool right = Physics.Raycast(currentHit.point, -Vector3.left, spacing / 2, layerMask);
					bool forward = Physics.Raycast(currentHit.point, Vector3.forward, spacing / 2, layerMask);
					bool back = Physics.Raycast(currentHit.point, -Vector3.forward, spacing / 2, layerMask);
					bool up = Physics.Raycast(currentHit.point, -Vector3.down, spacing / 2, layerMask);

					if (!left && !right && !forward && !back && !up)
					{
						hits.Add(currentHit);
					}

				}
				else
				{
					break;

				}
			}
		}

		return hits.ToArray();
	}

	// Locate neighbouring nodes
	private void FindNeighbors()
	{
		for (int o = 0; o < nodes.Length; o++)
		{
			OPNode thisNode = nodes[o];

			for (int i = 0; i < nodes.Length; i++)
			{
				OPNode thatNode = nodes[i];

				if ((thisNode.position - thatNode.position).sqrMagnitude <= spacing * 2.1)
				{
					thisNode.neighbors.Add(thatNode);
				}
			}
		}
	}
}
