﻿using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
	public HexGrid hexGrid;

	public Color[] colors;
	private Color activeColor;
	bool applyColor;

	int activeElevation;
	bool applyElevation = true;

	int brushSize;


	enum OptionalToggle   { Ignore, Yes, No } // yes aggiunge, no rimuove
	OptionalToggle riverMode, roadMode;

	//servono se devo trascinare da una cella ad un altra (tipo coi fiumi)
	bool isDrag;
	HexDirection dragDirection;
	HexCell previousCell;

	////////////////////////////////////////////////////////////////////


	void Awake()
	{
		SelectColor(0);
	}

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
			EditCells(currentCell);
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
			if (applyColor) {
				cell.Color = activeColor;
			}
			if (applyElevation) {
				cell.Elevation = activeElevation;
			}

			if (riverMode == OptionalToggle.No) {
				cell.RemoveRiver();
			}
			if (roadMode == OptionalToggle.No) {
				cell.RemoveRoads();
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

	void EditCells(HexCell center)
	{
		int centerX = center.coordinates.X;
		int centerZ = center.coordinates.Z;

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


	public void SelectColor(int index)
	{
		applyColor = index >= 0;
		if (applyColor) {
			activeColor = colors[index];
		}
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

	public void ShowUI(bool visible)
	{
		hexGrid.ShowUI(visible);
	}

	public void SetRiverMode(int mode)
	{
		riverMode = (OptionalToggle)mode;
	}

	public void SetRoadMode(int mode)
	{
		roadMode = (OptionalToggle)mode;
	}

}

