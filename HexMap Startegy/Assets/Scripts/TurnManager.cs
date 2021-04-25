using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] HexGrid hexGrid;

    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ //

    public void EndTurn()
    {
        foreach(HexUnit u in hexGrid.GetUnits())
        {
            u.DoTurn();
        }
    }


}
