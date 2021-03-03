using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
	Mesh hexMesh;
	MeshCollider meshCollider;
	
	List<Vector3> vertices;
	List<int> triangles;
	List<Color> colors;
	

	


	void Awake()
	{
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		meshCollider = gameObject.AddComponent<MeshCollider>();
		hexMesh.name = "Hex Mesh";
		vertices = new List<Vector3>();
		triangles = new List<int>();

		colors = new List<Color>();
	}

//cancella la mappa e ne fa una nuova
	public void Triangulate(HexCell[] cells)
	{
		hexMesh.Clear();
		vertices.Clear();
		triangles.Clear();
		colors.Clear();

		for (int i = 0; i < cells.Length; i++) {
			Triangulate(cells[i]);
		}
		hexMesh.vertices = vertices.ToArray();
		hexMesh.triangles = triangles.ToArray();
		hexMesh.colors = colors.ToArray();
		hexMesh.RecalculateNormals();

		meshCollider.sharedMesh = hexMesh;
	}

	//crea i 6 triangoli della cella
	void Triangulate(HexCell cell)
	{
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			Triangulate(d, cell);
		}
	}

	// aggiunge il triangolo nella direzione indicata
	// versione senza multi triangoli, rimuovere numeri
	void Triangulate123(HexDirection direction, HexCell cell)
	{
		Vector3 center = cell.Position;
		Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
		Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

		AddTriangle(center, v1, v2);
		AddTriangleColor(cell.color);

		if (direction <= HexDirection.SE) {
			TriangulateConnection(direction, cell, v1, v2);
		}
	}

	// versione con i multi triangoli
	void Triangulate(HexDirection direction, HexCell cell)
	{
		Vector3 center = cell.Position;
		EdgeVertices e = new EdgeVertices(
			center + HexMetrics.GetFirstSolidCorner(direction),
			center + HexMetrics.GetSecondSolidCorner(direction)
		);

		TriangulateEdgeFan(center, e, cell.color);

		if (direction <= HexDirection.SE) {
			TriangulateConnection(direction, cell, e);
		}
	}


	[System.Obsolete]//riempie i buchi tra le celle e collega altezze diverse
	void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2)
	{
		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null) {
			return;
		}

		Vector3 bridge = HexMetrics.GetBridge(direction);
		Vector3 v3 = v1 + bridge;
		Vector3 v4 = v2 + bridge;
		v3.y = v4.y = neighbor.Position.y;

		if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
			TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbor);
		} else {
			AddQuad(v1, v2, v3, v4);
			AddQuadColor(cell.color, neighbor.color);
		}

		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null) {
			Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
			v5.y = nextNeighbor.Position.y;

			if (cell.Elevation <= neighbor.Elevation) {
				if (cell.Elevation <= nextNeighbor.Elevation) {
					TriangulateCorner(v2, cell, v4, neighbor, v5, nextNeighbor);
				} else {
					TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
				}
			} else if (neighbor.Elevation <= nextNeighbor.Elevation) {
				TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, cell);
			} else {
				TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
			}
		}
	}

	//versione con multi triangoli
	void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
	{
		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null) {
			return;
		}

		Vector3 bridge = HexMetrics.GetBridge(direction);
		bridge.y = neighbor.Position.y - cell.Position.y;
		EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v4 + bridge);

		if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
			TriangulateEdgeTerraces(e1, cell, e2, neighbor);
		} else {
			TriangulateEdgeStrip(e1, cell.color, e2, neighbor.color);
		}

		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null) {
			Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
			v5.y = nextNeighbor.Position.y;

			if (cell.Elevation <= neighbor.Elevation) {
				if (cell.Elevation <= nextNeighbor.Elevation) {
					TriangulateCorner(e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor);
				} else {
					TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
				}
			} else if (neighbor.Elevation <= nextNeighbor.Elevation) {
				TriangulateCorner(e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell);
			} else {
				TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
			}
		}
	}
	//costruisce le terrazze
	void TriangulateEdgeTerraces(
		Vector3 beginLeft, Vector3 beginRight, HexCell beginCell,
		Vector3 endLeft, Vector3 endRight, HexCell endCell
	)
	{
		Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

		AddQuad(beginLeft, beginRight, v3, v4);
		AddQuadColor(beginCell.color, c2);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color c1 = c2;
			v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
			v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
			c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
			AddQuad(v1, v2, v3, v4);
			AddQuadColor(c1, c2);
		}

		AddQuad(v3, v4, endLeft, endRight);
		AddQuadColor(c2, endCell.color);
	}

	// versione multi triangolo
	void TriangulateEdgeTerraces(
		EdgeVertices begin, HexCell beginCell,
		EdgeVertices end, HexCell endCell
	)
	{
		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

		TriangulateEdgeStrip(begin, beginCell.color, e2, c2);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			EdgeVertices e1 = e2;
			Color c1 = c2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
			TriangulateEdgeStrip(e1, c1, e2, c2);
		}

		TriangulateEdgeStrip(e2, c2, end, endCell.color);
	}

	//aggusta gli angoli conle terrazze
	void TriangulateCorner(Vector3 bottom, HexCell bottomCell,Vector3 left, HexCell leftCell,Vector3 right, HexCell rightCell)
	{
		HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
		HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexEdgeType.Slope) {
			if (rightEdgeType == HexEdgeType.Slope) {
				TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
			} else if (rightEdgeType == HexEdgeType.Flat) {
				TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
			} else {
				TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
			}
		} else if (rightEdgeType == HexEdgeType.Slope) {
			if (leftEdgeType == HexEdgeType.Flat) {
				TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
			} else {
				TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
			}
		} else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			if (leftCell.Elevation < rightCell.Elevation) {
				TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
			} else {
				TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
			}
		} else {
			AddTriangle(bottom, left, right);
			AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
		}
	}

	void TriangulateCornerTerraces( Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
	{
		Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
		Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
		Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

		AddTriangle(begin, v3, v4);
		AddTriangleColor(beginCell.color, c3, c4);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color c1 = c3;
			Color c2 = c4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
			AddQuad(v1, v2, v3, v4);
			AddQuadColor(c1, c2, c3, c4);
		}

		AddQuad(v3, v4, left, right);
		AddQuadColor(c3, c4, leftCell.color, rightCell.color);
	}

	//attacca slope e cliff confinanti
	void TriangulateCornerTerracesCliff(
		Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell
		)
	{
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);  //senza perturb se tolgo i multi triangoli
		Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

		TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		} else {
			AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);  //senza perturb se tolgo i multi triangoli
			AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
		}
	}

	void TriangulateCornerCliffTerraces(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	)
	{
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);  //senza perturb se tolgo i multi triangoli
		Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

		TriangulateBoundaryTriangle(
			right, rightCell, begin, beginCell, boundary, boundaryColor
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		} else {
			AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);  //senza perturb se tolgo i multi triangoli
			AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
		}
	}

	void TriangulateBoundaryTriangle(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 boundary, Color boundaryColor
	)
	{
		Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));  //perturbato perche uso AddTriangleUnperturbed
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

		AddTriangleUnperturbed(Perturb(begin), v2, boundary);  //senza multi triangoli era AddTriangle
		AddTriangleColor(beginCell.color, c2, boundaryColor);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v2;
			Color c1 = c2;
			v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));  //perturbato perche uso AddTriangleUnperturbed
			c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			AddTriangleUnperturbed(v1, v2, boundary);  //senza multi triangoli era AddTriangle
			AddTriangleColor(c1, c2, boundaryColor);
		}

		AddTriangleUnperturbed(v2, Perturb(left), boundary);  //senza multi triangoli era AddTriangle
		AddTriangleColor(c2, leftCell.color, boundaryColor);
	}

	// un solo colore per triangolo
	void AddTriangleColor(Color color)
	{
		colors.Add(color);
		colors.Add(color);
		colors.Add(color);
	}

	//permette 3 colori per trinagolo
	void AddTriangleColor(Color c1, Color c2, Color c3)
	{
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
	}

	void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	//aggiunge un qudrilatero fatto con 2 triangoli
	void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		vertices.Add(Perturb(v4));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	//color un quadrilatero
	void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
	{
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
		colors.Add(c4);
	}

	void AddQuadColor(Color c1, Color c2)
	{
		colors.Add(c1);
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c2);
	}

	//serve a applicare rumore
	Vector3 Perturb(Vector3 position)
	{
		Vector4 sample = HexMetrics.SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
//		position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength; commento per mantenere le cose piatte
		position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
		return position;
	}


//  per fare i multi triangoli
	void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
	{
		AddTriangle(center, edge.v1, edge.v2);
		AddTriangleColor(color);
		AddTriangle(center, edge.v2, edge.v3);
		AddTriangleColor(color);
		AddTriangle(center, edge.v3, edge.v4);
		AddTriangleColor(color);
	}

	//sempre multi triangoli
	void TriangulateEdgeStrip(
		EdgeVertices e1, Color c1,
		EdgeVertices e2, Color c2
	)
	{
		AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		AddQuadColor(c1, c2);
		AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		AddQuadColor(c1, c2);
		AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		AddQuadColor(c1, c2);
	}

	//aggiunge triangolo senza perturbazioni, serve se uso multi triangoli per chiudere i buchi
	void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}


}