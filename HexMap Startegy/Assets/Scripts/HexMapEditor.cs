﻿using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
	public HexGrid hexGrid;

	int activeElevation, activeWaterLevel, activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;
	int activeTerrainTypeIndex;
	bool applyElevation = true, applyWaterLevel = true;
	bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;
	bool editMode;
	int brushSize;

	public Material terrainMaterial;

	enum OptionalToggle   { Ignore, Yes, No } // yes aggiunge, no rimuove
	OptionalToggle riverMode, roadMode, walledMode;

	//servono se devo trascinare da una cella ad un altra (tipo coi fiumi)
	bool isDrag;
	HexDirection dragDirection;
	HexCell previousCell;

	////////////////////////////////////////////////////////////////////



	void Update()
	{
		if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) { //blocca i click sui canvas perchè
			HandleInput();                                                                                                         // l'event system vede solo l'UI
		} else {																		
			previousCell = null;
		}
	}

	void HandleInput()
	{
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit)) {
			HexCell currentCell = hexGrid.GetCell(hit.point);
			if (previousCell && previousCell != currentCell) {
				ValidateDrag(currentCell);
			} else {
				isDrag = false;
			}

			if (editMode)
			{
				EditCells(currentCell);
			}
			else
			{
				hexGrid.FindDistancesTo(currentCell);
			}
			previousCell = currentCell;
		} else {
			previousCell = null;
		}
	}

	void ValidateDrag(HexCell currentCell)
	{
		for ( dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++ ) {
			if (previousCell.GetNeighbor(dragDirection) == currentCell) {
				isDrag = true;
				return;
			}
		}
		isDrag = false;
	}

	void EditCell(HexCell cell)
	{
		if (cell) {
			if (activeTerrainTypeIndex >= 0) {
				cell.TerrainTypeIndex = activeTerrainTypeIndex;
			}
			if (applyElevation) {
				cell.Elevation = activeElevation;
			}
			if (applyWaterLevel) {
				cell.WaterLevel = activeWaterLevel;
			}

			if (applySpecialIndex) {
				cell.SpecialIndex = activeSpecialIndex;
			}
			if (applyUrbanLevel) {
				cell.UrbanLevel = activeUrbanLevel;
			}
			if (applyFarmLevel) {
				cell.FarmLevel = activeFarmLevel;
			}
			if (applyPlantLevel) {
				cell.PlantLevel = activePlantLevel;
			}

			if (riverMode == OptionalToggle.No) {
				cell.RemoveRiver();
			}
			if (roadMode == OptionalToggle.No) {
				cell.RemoveRoads();
			}

			if (walledMode != OptionalToggle.Ignore) {
				cell.Walled = walledMode == OptionalToggle.Yes;
			}

			if (isDrag) {
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell) {
					if (riverMode == OptionalToggle.Yes) {
						otherCell.SetOutgoingRiver(dragDirection);
					}
					if (roadMode == OptionalToggle.Yes) {
						otherCell.AddRoad(dragDirection);
					}
				}
			}
		}
	}

	//per quando uso brush
	void EditCells(HexCell center)
	{
		int centerX = center.coordinates.x;
		int centerZ = center.coordinates.z;

		for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {// riga centrale e metà sotto
			for (int x = centerX - r; x <= centerX + brushSize; x++) {
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
		for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {// metà sopra
			for (int x = centerX - brushSize; x <= centerX + r; x++) {
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
	}


	void Awake()
	{
		terrainMaterial.DisableKeyword("GRID_ON");
	}

	////////////////////////////////////////////
	#region metodi UI (Set)

	public void SetTerrainTypeIndex(int index)
	{
		activeTerrainTypeIndex = index;
	}

	public void SetElevation(float elevation)
	{
		activeElevation = (int)elevation;
	}

	public void SetApplyElevation(bool toggle)
	{
		applyElevation = toggle;
	}

	public void SetBrushSize(float size)
	{
		brushSize = (int)size;
	}

	/*
	 * rimosso perchè l'ui viene accesa\spenta con l'edit mode
	public void ShowUI(bool visible)
	{
		hexGrid.ShowUI(visible);
	}
	*/

	public void SetRiverMode(int mode)
	{
		riverMode = (OptionalToggle)mode;
	}

	public void SetRoadMode(int mode)
	{
		roadMode = (OptionalToggle)mode;
	}

	public void SetApplyWaterLevel(bool toggle)
	{
		applyWaterLevel = toggle;
	}

	public void SetWaterLevel(float level)
	{
		activeWaterLevel = (int)level;
	}

	public void SetApplyUrbanLevel(bool toggle)
	{
		applyUrbanLevel = toggle;
	}

	public void SetUrbanLevel(float level)
	{
		activeUrbanLevel = (int)level;
	}

	public void SetApplyFarmLevel(bool toggle)
	{
		applyFarmLevel = toggle;
	}

	public void SetFarmLevel(float level)
	{
		activeFarmLevel = (int)level;
	}

	public void SetApplyPlantLevel(bool toggle)
	{
		applyPlantLevel = toggle;
	}

	public void SetPlantLevel(float level)
	{
		activePlantLevel = (int)level;
	}

	public void SetWalledMode(int mode)
	{
		walledMode = (OptionalToggle)mode;
	}

	public void SetApplySpecialIndex(bool toggle)
	{
		applySpecialIndex = toggle;
	}

	public void SetSpecialIndex(float index)
	{
		activeSpecialIndex = (int)index;
	}

	//attiva e disattiva la griglia
	public void ShowGrid(bool visible)
	{
		if (visible)
		{
			terrainMaterial.EnableKeyword("GRID_ON");
		}
		else
		{
			terrainMaterial.DisableKeyword("GRID_ON");
		}
	}

	public void SetEditMode(bool toggle)
	{
		editMode = toggle;
		hexGrid.ShowUI(!toggle);
		ShowGrid(toggle);

	}



	#endregion






}

