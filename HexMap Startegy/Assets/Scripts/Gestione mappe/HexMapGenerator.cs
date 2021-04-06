using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
	int cellCount;
	HexCellPriorityQueue searchFrontier;
	int searchFrontierPhase;

	public HexGrid grid;

	[Space(10)]

	public int seed;
	public bool useFixedSeed;

	[Range(0f, 0.5f)]
	public float jitterProbability = 0.25f;

	[Range(20, 200)]
	public int chunkSizeMin = 30;

	[Range(20, 200)] //non centra coi chunk per le mesh
	public int chunkSizeMax = 100;

	[Range(5, 95)]
	public int landPercentage = 50;

	[Range(1, 5)] //se è alto ci sono poche pianure e terra sparagliata
	public int waterLevel = 3;

	[Range(0f, 1f)] // se alta tende a ceare altipiani separati da Cliff
	public float highRiseProbability = 0.25f;

	[Range(0f, 0.8f)] //melgio non superare lo 0.5
	public float sinkProbability = 0.2f;

	[Range(-4, 0)]
	public int elevationMinimum = -2;
	//potrebbero non coincidere con gli slider dell'editor
	[Range(6, 10)]
	public int elevationMaximum = 8;

	public void GenerateMap(int x, int z)
	{
		Random.State originalRandomState = Random.state;
		if (!useFixedSeed)
		{
			seed = Random.Range(0, int.MaxValue);
			seed ^= (int)System.DateTime.Now.Ticks;
			seed ^= (int)Time.time;
			seed &= int.MaxValue;
		}
		Random.InitState(seed);

		cellCount = x * z;
		grid.CreateMap(x, z);
		if (searchFrontier == null)
		{
			searchFrontier = new HexCellPriorityQueue();
		}

		for (int i = 0; i < cellCount; i++)
		{
			grid.GetCell(i).WaterLevel = waterLevel;
		}
		CreateLand();
		SetTerrainType();

		for (int i = 0; i < cellCount; i++)
		{// mette a zero per evitare problemi futuri
			grid.GetCell(i).SearchPhase = 0;
		}

		Random.state = originalRandomState;
	}

	//prende una cella a caso e alza il terreno di quella e delle (chunkSize - 1) attorno
	int RaiseTerrain(int chunkSize, int budget)
	{
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell();
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
		HexCoordinates center = firstCell.coordinates;

		int rise = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			int originalElevation = current.Elevation;
			int newElevation = originalElevation + rise;
			if (newElevation > elevationMaximum)
			{//non supero l'elevazione massima
				continue;
			}
			current.Elevation = newElevation;
			if ( originalElevation < waterLevel && newElevation >= waterLevel && --budget == 0)
			{ // finisco se raggiungo il budget
				break;
			}
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.coordinates.DistanceTo(center);
					neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;  // aggiuge casualità alla forma presa
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();
		return budget;
	}

	int SinkTerrain(int chunkSize, int budget)
	{
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell();
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
		HexCoordinates center = firstCell.coordinates;

		int sink = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			int originalElevation = current.Elevation;
			int newElevation = current.Elevation - sink;
			if (newElevation < elevationMinimum)
			{
				continue;
			}
			current.Elevation = newElevation;
			if (originalElevation >= waterLevel && newElevation < waterLevel	)
			{//se sommergo una cella dovrò farne emergere un'altra
				budget += 1;
			}
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
				{
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.coordinates.DistanceTo(center);
					neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;  // aggiuge casualità alla forma presa
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();
		return budget;
	}

	void SetTerrainType()
	{
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			if (!cell.IsUnderwater)
			{
				cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
			}
		}
	}

	HexCell GetRandomCell()
	{
		return grid.GetCell(Random.Range(0, cellCount));
	}

	//genera terreno finchè non raggiunge la quantità del budget
	void CreateLand()
	{
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
		while (landBudget > 0)
		{
			int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax + 1);
			if (Random.value < sinkProbability)
			{
				landBudget = SinkTerrain(chunkSize, landBudget);
			}
			else
			{
				landBudget = RaiseTerrain(chunkSize, landBudget);
			}
		}
	}
}
