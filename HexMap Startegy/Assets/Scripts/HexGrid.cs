using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
	public int cellCountX, cellCountZ; //dimesioni mappa, devono essere multipli dele dimensioni dei chunks
	int chunkCountX, chunkCountZ;

	public HexGridChunk chunkPrefab;
	HexGridChunk[] chunks;
	public HexCell cellPrefab;
	HexCell[] cells;
	     
	public Text cellLabelPrefab;

	public Color touchedColor = Color.magenta;

	public Texture2D noiseSource;

	public int seed;
	/// //////////////////////////////////////////////////////////////

	void OnEnable()
	{
		if (!HexMetrics.noiseSource) {
			HexMetrics.noiseSource = noiseSource;
			HexMetrics.InitializeHashGrid(seed);
		}
	}

	void Awake()
	{
		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitializeHashGrid(seed);
		CreateMap(cellCountX, cellCountZ);
	}

	public bool CreateMap(int x, int z)
	{
		if (// dimensioni in celle deve essere multiple dimensioni chunks
			x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
			z <= 0 || z % HexMetrics.chunkSizeZ != 0
		) {
			Debug.LogError("Unsupported map size.");
			return false;
		}

		if (chunks != null) {
			for (int i = 0; i < chunks.Length; i++) {
				Destroy(chunks[i].gameObject);
			}
		}

		cellCountX = x;
		cellCountZ = z;
		chunkCountX = cellCountX / HexMetrics.chunkSizeX;
		chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
		CreateChunks();
		CreateCells();

        return true;
	}

	void CreateChunks()
	{
		chunks = new HexGridChunk[chunkCountX * chunkCountZ];

		for (int z = 0, i = 0; z < chunkCountZ; z++) {
			for (int x = 0; x < chunkCountX; x++) {
				HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
				chunk.transform.SetParent(transform);
			}
		}
	}

	void CreateCells()
	{
		cells = new HexCell[cellCountZ * cellCountX];

		for (int z = 0, i = 0; z < cellCountZ; z++) {
			for (int x = 0; x < cellCountX; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	void CreateCell(int x, int z, int i)
	{
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

		//imposta i vicini
		if (x > 0) {
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
		}
		if (z > 0) {
			if ((z & 1) == 0) {//modo figo per prendere i numeri dispari
				cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
				if (x > 0) {
					cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
				}
			} else {
				cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
				if (x < cellCountX - 1) {
					cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
				}
			}

		}

		//aggiunge testo
		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		//label.text = cell.coordinates.ToStringOnSeparateLines();
		cell.uiRect = label.rectTransform; //aggiusta posizione etichetta

		cell.Elevation = 0;
		AddCellToChunk(x, z, cell);
	}

	void AddCellToChunk(int x, int z, HexCell cell)
	{
		int chunkX = x / HexMetrics.chunkSizeX;
		int chunkZ = z / HexMetrics.chunkSizeZ;
		HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

		int localX = x - chunkX * HexMetrics.chunkSizeX;
		int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
		chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
	}


	public HexCell GetCell(Vector3 position)
	{
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		int index = coordinates.x + coordinates.z * cellCountX + coordinates.z / 2;
		return cells[index];
	}

	public HexCell GetCell(HexCoordinates coordinates)
	{
		int z = coordinates.z;
		if (z < 0 || z >= cellCountZ)   return null;

		int x = coordinates.x + z / 2;
		if (x < 0 || x >= cellCountX)   return null;

		return cells[x + z * cellCountX];
	}

	public void ShowUI(bool visible)
	{
		for (int i = 0; i < chunks.Length; i++) {
			chunks[i].ShowUI(visible);
		}
	}


	public void FindDistancesTo(HexCell cell)
	{
		StopAllCoroutines();
		StartCoroutine(Search(cell));
	}

	IEnumerator Search(HexCell cell)
	{//fa Dijkstra
		for (int i = 0; i < cells.Length; i++)
		{//inizializza le distanze al maassimo
			cells[i].Distance = int.MaxValue;
		}

		WaitForSeconds delay = new WaitForSeconds(1 / 60f);
		List<HexCell> frontier = new List<HexCell>();
		cell.Distance = 0;
		frontier.Add(cell);

		while (frontier.Count > 0)
		{
			yield return delay;
			HexCell current = frontier[0];
			frontier.RemoveAt(0);

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor == null)
				{// se le condizioni sono verificate (almeno 1) non considera questo vicino
					continue;
				}
				if (neighbor.IsUnderwater)
				{//come sopra
					continue;
				}

				HexEdgeType edgeType = current.GetEdgeType(neighbor);
				if (edgeType == HexEdgeType.Cliff)
				{
					continue;
				}

				int distance = current.Distance;
				if (current.HasRoadThroughEdge(d))
				{
					distance += 1;
				}
				else if (current.Walled != neighbor.Walled)
				{//non attraverso i muri se manca la strada
					continue;
				}
				else
				{
					distance += (edgeType == HexEdgeType.Flat) ? 5 : 10;
					distance += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
				}

				if (neighbor.Distance == int.MaxValue)
				{
					neighbor.Distance = distance;
					frontier.Add(neighbor);
				}
				else if (distance < neighbor.Distance)
				{
					neighbor.Distance = distance;
				}
				frontier.Sort((x, y) => x.Distance.CompareTo(y.Distance));
			}
		}
	}








	public void Save(BinaryWriter writer)
	{
		writer.Write(cellCountX);
		writer.Write(cellCountZ);

		for (int i = 0; i < cells.Length; i++) {
			cells[i].Save(writer);
		}
	}

	public void Load(BinaryReader reader, int header)
	{
		StopAllCoroutines();

		int x = 18, z = 18;
		if (header >= 1) {// serve se uso mappe vechie (header=0) che non avevano la dimensione
			x = reader.ReadInt32();
			z = reader.ReadInt32();
		}

		if (x != cellCountX || z != cellCountZ) { //se hanno la stessa dimensione non creo una nuova mappa
			if (!CreateMap(x, z)) {
				return;
			}
		}

		for (int i = 0; i < cells.Length; i++) {
			cells[i].Load(reader);
		}
		for (int i = 0; i < chunks.Length; i++) {
			chunks[i].Refresh();
		}
	}







}