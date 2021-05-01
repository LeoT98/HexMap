using UnityEngine;

public class HexPlayer
{
    [HideInInspector] public int index;

    public int gainedMP, gainedSteel;
    public int manpower, steel;


    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ //

    public HexPlayer(int i)
    {
        index = i;
        manpower = 0;
        steel = 0;
        gainedMP = 0;
        gainedSteel = 0;
        
    }

    public void DoTurn()
    {
        manpower += gainedMP;
        steel += gainedSteel;
    }
}
