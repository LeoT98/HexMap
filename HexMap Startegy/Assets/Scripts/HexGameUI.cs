using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
    public HexGrid grid;
	HexCell currentCell;
	HexUnit selectedUnit;
	[SerializeField] Canvas canvasGioco;




	// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ //


	void Update()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButtonDown(0))
			{
				DoSelection();
			}
			else if (selectedUnit)
			{
				if (Input.GetMouseButtonDown(1))
				{//se un'unità è selezionata la muove
					DoMove();
				}
				else
				{//se un'unità è selezionata disegna e calcola il pathfinding
					DoPathfinding();
				}
			}
		}
	}


	bool UpdateCurrentCell()
	{
		HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
		if (cell != currentCell)
		{// se sono uguali non serve aggiornare
			currentCell = cell;
			return true;
		}
		return false;
	}

	void DoSelection()
	{
		grid.ClearPath();
		UpdateCurrentCell();
		if (currentCell)
		{
			selectedUnit = currentCell.Unit;
		}
	}
		
	void DoPathfinding()
	{
		if (UpdateCurrentCell())
		{
			if (currentCell && selectedUnit.IsValidDestination(currentCell))
			{
				grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
			}
			else
			{
				grid.ClearPath();
			}
		}
	}

	void DoMove()
	{
		if (grid.HasPath)
		{
			selectedUnit.CreatePathsStack(grid.GetPath());
			selectedUnit.MoveOneTurn();
			grid.ClearPath();
		}
	}



	public void SetEditMode(bool toggle)
	{// si disattiva se vado  in EditMode e vice versa
		enabled = !toggle;
		grid.ShowUI(!toggle);
		grid.ClearPath();

		canvasGioco.gameObject.SetActive(!toggle);

		if (toggle)
		{
			Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
		}
		else
		{
			Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
		}
	}
}
