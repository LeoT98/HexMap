using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] HexGrid grid;
    [SerializeField] HexGameUI gameUI;

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ //

    public void EndTurn()
    {
        foreach(HexUnit u in grid.GetUnits())
        {
            u.DoTurn();
        }
        gameUI.UpdateUnitTexts();

        grid.players[0].DoTurn();
        gameUI.UpdateResources(grid.players[0].manpower, grid.players[0].steel);
    }


}
