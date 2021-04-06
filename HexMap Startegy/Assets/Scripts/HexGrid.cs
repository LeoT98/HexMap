using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
	[HideInInspector]
	public int cellCountX, cellCountZ; //dimesioni mappa, devono essere multipli dele dimensioni dei chunks
	int chunkCountX, chunkCountZ;
	public int seed;

	public HexGridChunk chunkPrefab;
	HexGridChunk[] chunks;
	public HexCell cellPrefab;
	HexCell[] cells;
	     
	public Text cellLabelPrefab;

	//public Color touchedColor = Color.magenta;

	public Texture2D noiseSource;

	public HexCellShaderData cellShaderData; //component aggiunto in Awake


	//Pathfinding
	HexCellPriorityQueue searchFrontier;
	int searchFrontierPhase;
	HexCell currentPathFrom, currentPathTo;
	public bool HasPath
	{
		get {
			return currentPathExists;
		}
	}
	bool currentPathExists;

	//Unità
	List<HexUnit> units = new List<HexUnit>();
	public HexUnit unitPrefab;

	/// //////////////////////////////////////////////////////////////

 	void OnEnable()
	{
		if (!HexMetrics.noiseSource) {
			HexMetrics.noiseSource = noiseSource;
			HexMetrics.InitializeHashGrid(seed);
			HexUnit.unitPrefab = unitPrefab;
			ResetVisibility();
		}
	}

	void Awake()
	{
		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitializeHashGrid(seed);
		HexUnit.unitPrefab = unitPrefab;
		cellShaderData = gameObject.AddComponent<HexCellShaderData>();
		cellShaderData.Grid = this;
		CreateMap(cellCountX, cellCountZ);
	}

	public bool CreateMap(int x, int z)
	{
		if ( x <= 0 || x % HexMetrics.chunkSizeX != 0 || z <= 0 || z % HexMetrics.chunkSizeZ != 0)
		{// dimensioni in celle deve essere multiple dimensioni chunks
			Debug.LogError("Unsupported map size.");
			return false;
		}

		ClearPath();
		ClearUnits();
		if (chunks != null) {
			for (int i = 0; i < chunks.Length; i++) {
				Destroy(chunks[i].gameObject);
			}
		}

		cellCountX = x;
		cellCountZ = z;
		chunkCountX = cellCountX / HexMetrics.chunkSizeX;
		chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
		cellShaderData.Initialize(cellCountX, cellCountZ);

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
		cell.Index = i;
		cell.ShaderData = cellShaderData;

		cell.Explorable = x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1; //bordi non esplorabili

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

	//spara raycast e ritorna la cella colpita.  Passargli: Camera.main.ScreenPointToRay(Input.mousePosition)   per selezionare col mouse
	public HexCell GetCell(Ray ray)
	{
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			return GetCell(hit.point);
		}
		return null;
	}

	public HexCell GetCell(int xOffset, int zOffset)
	{
		return cells[xOffset + zOffset * cellCountX];
	}

	public HexCell GetCell(int cellIndex)
	{
		return cells[cellIndex];
	}

	public void ShowUI(bool visible)
	{
		for (int i = 0; i < chunks.Length; i++) {
			chunks[i].ShowUI(visible);
		}
	}


	public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
	{
		ClearPath();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = Search(fromCell, toCell, unit);
		ShowPath(unit.Speed);
	}

	//fa il pathfinding
	bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
	{// se non ho abbastanza movimento per entrare in una cella non ci entro e spreco il movimento
		searchFrontierPhase += 2; //imposta il valore per cui una cella eè già stata contollata
		int speed = unit.Speed;
		if (searchFrontier == null)
		{// posso inizializzarlo nello start ed evitare l'if
			searchFrontier = new HexCellPriorityQueue();
		}
		else
		{//va pulito prima di ogni utilizzo
			searchFrontier.Clear();
		}

		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);

		while (searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;

			if (current == toCell)
			{//sono arrivato a destinazione
				return true;
			}

			int currentTurn = (current.Distance - 1) / speed;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase)
				{// se le condizioni sono verificate non considera questo vicino
					continue;
				}

				if (!unit.IsValidDestination(neighbor))
				{
					continue;
				}
				int moveCost = unit.GetMoveCost(current, neighbor, d);
				if (moveCost < 0)
				{
					continue;
				}

				int distance = current.Distance + moveCost;
				int turn = (distance - 1) / speed; //arrotonda per difetto
				if (turn > currentTurn)
				{
					distance = turn * speed + moveCost;
				}

				if (neighbor.SearchPhase < searchFrontierPhase)
				{//cella non ancora visitata
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;

					//Come euristica metto distanza minima perchè ipotizzo strada (costo 1).
					//Se lo aumento controlla meno starade e potrei perdere percorsi ottimi che però fanno un giro largo
					neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);

					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance)
				{//cella già visitata ma ho trovato percorso migliore
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;

					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		//se arrivo qui ho percorso tutte le strade possibili
		return false;
	}

	public List<HexCell> GetPath()
	{
		if (!currentPathExists)
		{
			return null;
		}
		List<HexCell> path = ListPool<HexCell>.Get();
		for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
		{
			path.Add(c);
		}
		path.Add(currentPathFrom); // il for non lo aggiunge
		path.Reverse(); //lo inverte così va dalla partenza all'arrivo
		return path;
	}

	//disegna percorso sulla mappa
	void ShowPath(int speed)
	{
		if (currentPathExists)
		{
			HexCell current = currentPathTo;
			while (current != currentPathFrom)
			{
				int turn = (current.Distance - 1) / speed;
				current.SetLabel(turn.ToString());
				current.EnableHighlight(Color.white);
				current = current.PathFrom;
			}
		}
		currentPathFrom.EnableHighlight(Color.blue);
		currentPathTo.EnableHighlight(Color.red);
	}

	//cancella percorso dalla mappa
	public void ClearPath()
	{
		if (currentPathExists)
		{
			HexCell current = currentPathTo;
			while (current != currentPathFrom)
			{
				current.SetLabel(null);
				current.DisableHighlight();
				current = current.PathFrom;
			}
			current.DisableHighlight();
			currentPathExists = false;
		}
		else if (currentPathFrom)
		{
			currentPathFrom.DisableHighlight();
			currentPathTo.DisableHighlight();
		}
		currentPathFrom = currentPathTo = null;
	}

	//cancella tutte le unità
	void ClearUnits()
	{
		for (int i = 0; i < units.Count; i++)
		{
			units[i].Die();
		}
		units.Clear();
	}

	public void AddUnit(HexUnit unit, HexCell location, float orientation)
	{
		units.Add(unit);
		unit.Grid = this;
		unit.transform.SetParent(transform, false);
		unit.Location = location;
		unit.Orientation = orientation;
	}

	public void RemoveUnit(HexUnit unit)
	{
		units.Remove(unit);
		unit.Die();
	}

	//fa tipo pathfinding ma per la visibilità
	List<HexCell> GetVisibleCells(HexCell fromCell, int range)
	{
		List<HexCell> visibleCells = ListPool<HexCell>.Get();

		searchFrontierPhase += 2;
		if (searchFrontier == null)
		{
			searchFrontier = new HexCellPriorityQueue();
		}
		else
		{
			searchFrontier.Clear();
		}

		range += fromCell.ViewElevation;
		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);

		HexCoordinates fromCoordinates = fromCell.coordinates;
		while (searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;
			visibleCells.Add(current);

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase)
				{
					continue;
				}
				int distance = current.Distance + 1; //distanza del vicino che sto controllando
				if (distance + neighbor.ViewElevation > range || 
					distance > fromCoordinates.DistanceTo(neighbor.coordinates) || !neighbor.Explorable)
				{
					continue;
				}

				if (neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.SearchHeuristic = 0;  //viene usato da searchFrontier per stabilire la priorità
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance)
				{
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return visibleCells;
	}

	public void IncreaseVisibility(HexCell fromCell, int range)
	{
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++)
		{
			cells[i].IncreaseVisibility();
		}
		ListPool<HexCell>.Add(cells);
	}

	public void DecreaseVisibility(HexCell fromCell, int range)
	{
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++)
		{
			cells[i].DecreaseVisibility();
		}
		ListPool<HexCell>.Add(cells);
	}

	public void ResetVisibility()
	{
		for (int i = 0; i < cells.Length; i++)
		{
			cells[i].ResetVisibility();
		}
		for (int i = 0; i < units.Count; i++)
		{
			HexUnit unit = units[i];
			IncreaseVisibility(unit.Location, unit.VisionRange);
		}
	}





	public void Save(BinaryWriter writer)
	{
		writer.Write(cellCountX);
		writer.Write(cellCountZ);

		for (int i = 0; i < cells.Length; i++) 
		{// slava la mappa
			cells[i].Save(writer);
		}

		writer.Write(units.Count);
		for (int i = 0; i < units.Count; i++)
		{// salva le unità
			units[i].Save(writer);
		}
	}

	public void Load(BinaryReader reader, int header)
	{
		ClearPath();
		ClearUnits();

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

		bool originalImmediateMode = cellShaderData.ImmediateMode;
		cellShaderData.ImmediateMode = true; // evita che ci sia transizone durante il caricamento

		for (int i = 0; i < cells.Length; i++) 
		{// modifica i valori delle celle
			cells[i].Load(reader, header);
		}
		for (int i = 0; i < chunks.Length; i++)
		{// aggiorna le mesh
			chunks[i].Refresh();
		}

		if (header >= 2)
		{// le unità sono state aggiunte dalla versione 2
			int unitCount = reader.ReadInt32();
			for (int i = 0; i < unitCount; i++)
			{// mette le unità
				HexUnit.Load(reader, this);
			}
		}

		cellShaderData.ImmediateMode = originalImmediateMode;
	}







}