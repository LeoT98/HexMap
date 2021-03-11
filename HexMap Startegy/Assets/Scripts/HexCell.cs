using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    public HexGridChunk chunk; //chunk in cui è contenuta
    public RectTransform uiRect; //riferimento alla posizione dell'etichetta

    [SerializeField]
    HexCell[] neighbors;

    public int Elevation {
        get {
            return elevation;
        }
        set {
            if (elevation == value)    return;

            elevation = value;
			Vector3 position = transform.localPosition;
			position.y = value * HexMetrics.elevationStep;
			position.y += // causa rumore sull'altezza
				(HexMetrics.SampleNoise(position).y * 2f - 1f)  * HexMetrics.elevationPerturbStrength;
			transform.localPosition = position;

			Vector3 uiPosition = uiRect.localPosition;
			uiPosition.z = -position.y;
			uiRect.localPosition = uiPosition;

            //previene fiumi che vanno in salita
            if( hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation) {
                RemoveOutgoingRiver();
            }
            if( hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation) {
                RemoveIncomingRiver();
            }

            //rimuove strade se c'è troppa diffrenza d'altezza
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection)i) > roadMaxDeltaElevation) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }
    int elevation=-1;

    public Color Color {
        get {
            return color;
        }
        set {
            if (color == value) {
                return;
            }
            color = value;
            Refresh();
        }
    }
    Color color;

    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    #region FIUMI
    public bool HasIncomingRiver {
        get {
            return hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver {
        get {
            return hasOutgoingRiver;
        }
    }
    bool hasIncomingRiver, hasOutgoingRiver;

    public HexDirection IncomingRiver {
        get {
            return incomingRiver;
        }
    }

    public HexDirection OutgoingRiver {
        get {
            return outgoingRiver;
        }
    }
    HexDirection incomingRiver, outgoingRiver;

    public bool HasRiver {
        get {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd {
        get {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }

    public float StreamBedY { //altezza del fondo del fiume
        get {
            return   (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }

    public float RiverSurfaceY {
        get {
            return  (elevation + HexMetrics.riverSurfaceElevationOffset)  * HexMetrics.elevationStep;
        }
    }

    public HexDirection RiverBeginOrEndDirection {
        get {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    #endregion

    //Strade
    [SerializeField]
    bool[] roads;  //nell inspector metterlo con size=6
    [SerializeField]
    int roadMaxDeltaElevation = 1; //max differenza di altezza per cui posso mettere una strada

    public bool HasRoads {
        get {
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i]) {
                    return true;
                }
            }
            return false;
        }
    }

    /////////////////////////////////////////////////////////////


    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    void Refresh()
    {
        if (chunk) {
            chunk.Refresh();

            for (int i = 0; i < neighbors.Length; i++) {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    void RefreshSelfOnly()
    {
        chunk.Refresh();
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
           ( hasIncomingRiver && incomingRiver == direction ) || ( hasOutgoingRiver && outgoingRiver == direction );
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver) {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver) {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        //prima controlla seposso aggiungere il fiume in questa direzione
        if (hasOutgoingRiver && outgoingRiver == direction) {
            return;
        }
        HexCell neighbor = GetNeighbor(direction);
        if (!neighbor || elevation < neighbor.elevation) {
            return;
        }

        // rimuove il vecchio fiume
        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction) {
            RemoveIncomingRiver();
        }

        //aggiunge nuovo fiume
        hasOutgoingRiver = true;
        outgoingRiver = direction;
 //    RefreshSelfOnly();  ci pensa la strada a refreshare
        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        //  neighbor.RefreshSelfOnly();  ci pensa la strada a refreshare

        SetRoad((int)direction, false);  //si occupa anche dei refresh;
    }

    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= roadMaxDeltaElevation) {
            SetRoad((int)direction, true);
        }
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++) {
            if (roads[i]) {
                SetRoad(i, false);
            }
        }
    }

    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }





}