﻿using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
	int cellCount;
	HexCellPriorityQueue searchFrontier;
	int searchFrontierPhase;
	public HexGrid grid;

	List<MapRegion> regions;
	List<ClimateData> climate = new List<ClimateData>();
	List<ClimateData> nextClimate = new List<ClimateData>();// serve per rendere le cose "parallele"

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

	[Range(0, 12)]
	public int mapBorderX = 4;
	// influenzano il centro di generazione, non impediscono ci sia terra vicino ai bordi.
	[Range(0, 12)]
	public int mapBorderZ = 4;

	[Range(0, 10)] // si aggiunge al mapBorder ma solo per i confini tra regioni
	public int regionBorder = 4;

	[Range(1, 4)]
	public int regionCount = 1;

	[Range(0, 100)]
	public int erosionPercentage = 50;

	[Range(0,120)]
	public int climateSimulation = 40; //quante volte simulo il ciclo dell' acqua. Default 40

	[Range(0f, 1f)]
	public float evaporationFactor = 0.5f;

	[Range(0f, 1f)] // quante nuvole diventano pioggia. Indica la percentuale
	public float precipitationFactor = 0.25f;

	[Range(0f, 1f)] // drenaggio del terreno dovuto alla gravità
	public float runoffFactor = 0.25f;

	[Range(0f, 1f)] // moisture che la terra assorbe dai vicini a pari altezza
	public float seepageFactor = 0.125f;

	[Range(0f, 1f)]// moisture iniziale per le celle di terra
	public float startingMoisture = 0.1f;

	public HexDirection windDirection = HexDirection.NW;

	[Range(1f, 10f)] // 1 indica niente vento
	public float windStrength = 4f;




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

		CreateRegions();
		CreateLand();
		ErodeLand();
		CreateClimate();
		SetTerrainType();

		for (int i = 0; i < cellCount; i++)
		{// mette a zero per evitare problemi futuri
			grid.GetCell(i).SearchPhase = 0;
		}

		Random.state = originalRandomState;
	}

	//prende una cella a caso e alza il terreno di quella e delle (chunkSize - 1) attorno
	int RaiseTerrain(int chunkSize, int budget, MapRegion region)
	{
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell(region);
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

	int SinkTerrain(int chunkSize, int budget, MapRegion region)
	{
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell(region);
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
			float moisture = climate[i].moisture;
			if (!cell.IsUnderwater)
			{
				if (moisture < 0.05f)
				{
					cell.TerrainTypeIndex = 4;
				}
				else if (moisture < 0.12f)
				{
					cell.TerrainTypeIndex = 0;
				}
				else if (moisture < 0.28f)
				{
					cell.TerrainTypeIndex = 3;
				}
				else if (moisture < 0.85f)
				{
					cell.TerrainTypeIndex = 1;
				}
				else
				{
					cell.TerrainTypeIndex = 2;
				}
			}
			else
			{
				cell.TerrainTypeIndex = 0;
			}
		}
	}

	HexCell GetRandomCell(MapRegion region)
	{
		return grid.GetCell(Random.Range(region.xMin, region.xMax), Random.Range(region.zMin, region.zMax));
	}

	//genera terreno finchè non raggiunge la quantità del budget
	void CreateLand()
	{
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
		for (int guard = 0; guard < 10000; guard++)
		{// cicla finchè ha budget o per un numero massimo di volte (guard) che impedisce di bloccarsi su mappe impossibili
			bool sink = Random.value < sinkProbability;
			for (int i = 0; i < regions.Count; i++)
			{
				MapRegion region = regions[i];
				int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
				if (sink)
				{
					landBudget = SinkTerrain(chunkSize, landBudget, region);
				}
				else
				{
					landBudget = RaiseTerrain(chunkSize, landBudget, region);
					if (landBudget <= 0) //nell'originale è ==
					{
						return;
					}
				}
			}
		}

		if (landBudget > 0)
		{// non è riuscito a creare la mappa perchè ha finito le iterazioni
			Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
		}
	}

	void CreateRegions()
	{
		if (regions == null)
		{
			regions = new List<MapRegion>();
		}
		else
		{
			regions.Clear();
		}

		MapRegion region;
		switch (regionCount)
		{
			default:
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX - mapBorderX;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				break;
/////////////////////////////////////////////////////////////////////
			case 2:
				if (Random.value < 0.5f)
					{//regioni divise destra-sinistra
						region.xMin = mapBorderX;
						region.xMax = grid.cellCountX / 2 - regionBorder;
						region.zMin = mapBorderZ;
						region.zMax = grid.cellCountZ - mapBorderZ;
						regions.Add(region);
						region.xMin = grid.cellCountX / 2 + regionBorder;
						region.xMax = grid.cellCountX - mapBorderX;
						regions.Add(region);
				}
				else
				{// regioni divise sopra-sotto
					region.xMin = mapBorderX;
					region.xMax = grid.cellCountX - mapBorderX;
					region.zMin = mapBorderZ;
					region.zMax = grid.cellCountZ / 2 - regionBorder;
					regions.Add(region);
					region.zMin = grid.cellCountZ / 2 + regionBorder;
					region.zMax = grid.cellCountZ - mapBorderZ;
					regions.Add(region);
				}
				break;
/////////////////////////////////////////////////////////////////////
			case 3:
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 3 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = grid.cellCountX / 3 + regionBorder;
				region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
				regions.Add(region);
				region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
				region.xMax = grid.cellCountX - mapBorderX;
				regions.Add(region);
				break;
//////////////////////////////////////////////////////////////////////
			case 4:
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 2 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ / 2 - regionBorder;
				regions.Add(region);
				region.xMin = grid.cellCountX / 2 + regionBorder;
				region.xMax = grid.cellCountX - mapBorderX;
				regions.Add(region);
				region.zMin = grid.cellCountZ / 2 + regionBorder;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 2 - regionBorder;
				regions.Add(region);
				break;
//////////////////////////////////////////////////////////////////////////////
			
		}
	}

	void ErodeLand() 
	{
		List<HexCell> erodibleCells = ListPool<HexCell>.Get();
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			if (IsErodible(cell))
			{
				erodibleCells.Add(cell);
			}
		}
		int targetErodibleCount = (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);
		while (erodibleCells.Count > targetErodibleCount)
		{
			int index = Random.Range(0, erodibleCells.Count);
			HexCell cell = erodibleCells[index];
			HexCell targetCell = GetErosionTarget(cell);
			cell.Elevation -= 1;
			targetCell.Elevation += 1;

			if (!IsErodible(cell))
			{
				erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
				erodibleCells.RemoveAt(erodibleCells.Count - 1);
			}

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				HexCell neighbor = cell.GetNeighbor(d);
				if (neighbor && neighbor.Elevation == cell.Elevation + 2 && !erodibleCells.Contains(neighbor))
				{
					erodibleCells.Add(neighbor);
				}
			}

			if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
			{
				erodibleCells.Add(targetCell);
			}

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{//controlla se dopo le modifiche sono cambiate le celle da erodere
				HexCell neighbor = targetCell.GetNeighbor(d);
				if (neighbor && neighbor != cell && neighbor.Elevation == targetCell.Elevation + 1 && !IsErodible(neighbor))
				{
					erodibleCells.Remove(neighbor);
				}
			}
		}

		ListPool<HexCell>.Add(erodibleCells);
	}

	bool IsErodible(HexCell cell)
	{// erode se ha un vicino con confine Cliff. Considera anche le celle sommerse
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation)
			{
				return true;
			}
		}
		return false;
	}

	//se erodo una cella la materia dovrà andare da un'altra parte
	HexCell GetErosionTarget(HexCell cell)
	{
		List<HexCell> candidates = ListPool<HexCell>.Get();
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation)
			{
				candidates.Add(neighbor);
			}
		}
		HexCell target = candidates[Random.Range(0, candidates.Count)];
		ListPool<HexCell>.Add(candidates);
		return target;
	}

	void CreateClimate()
	{
		climate.Clear();
		nextClimate.Clear();
		ClimateData initialData = new ClimateData();
		initialData.moisture = startingMoisture;
		ClimateData clearData = new ClimateData();
		for (int i = 0; i < cellCount; i++)
		{
			climate.Add(initialData);
			nextClimate.Add(clearData);
		}

		for (int cycle = 0; cycle < climateSimulation; cycle++)
		{
			for (int i = 0; i < cellCount; i++)
			{
				EvolveClimate(i);
			}
			List<ClimateData> swap = climate;
			climate = nextClimate;
			nextClimate = swap;
		}
	}

	void EvolveClimate(int cellIndex)
	{
		HexCell cell = grid.GetCell(cellIndex);
		ClimateData cellClimate = climate[cellIndex];

		if (cell.IsUnderwater)
		{
			cellClimate.moisture = 1f;
			cellClimate.clouds += evaporationFactor;
		}
		else
		{
			float evaporation = cellClimate.moisture * evaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}


		float precipitation = cellClimate.clouds * precipitationFactor;
		cellClimate.clouds -= precipitation;
		cellClimate.moisture += precipitation;

		float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);//l'altezza limita le nuvole
		if (cellClimate.clouds > cloudMaximum)
		{//se ci sono troppe nuvole piove (meno clouds, più moisture)
			cellClimate.moisture += cellClimate.clouds - cloudMaximum;
			cellClimate.clouds = cloudMaximum;
		}

		HexDirection mainDispersalDirection = windDirection;
		float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
		float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
		float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (!neighbor)
			{
				continue;
			}

			ClimateData neighborClimate = nextClimate[neighbor.Index];
			if (d == mainDispersalDirection)
			{
				neighborClimate.clouds += cloudDispersal * windStrength;
			}
			else
			{
				neighborClimate.clouds += cloudDispersal;
			}

			int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
			if (elevationDelta < 0)
			{//runoff solo verso il basso
				cellClimate.moisture -= runoff;
				neighborClimate.moisture += runoff;
			}
			else if (elevationDelta == 0)
			{
				cellClimate.moisture -= seepage;
				neighborClimate.moisture += seepage;
			}

			nextClimate[neighbor.Index] = neighborClimate;
		}

		ClimateData nextCellClimate = nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;
		if (nextCellClimate.moisture > 1f)
		{// non necessario, serve per evitare che la terra abbia più moisture dell'acqua
			nextCellClimate.moisture = 1f;
		}
		nextClimate[cellIndex] = nextCellClimate;
		climate[cellIndex] = new ClimateData();
	}



}




struct MapRegion
{
	public int xMin, xMax, zMin, zMax;
}

struct ClimateData
{
	public float clouds, moisture;
}