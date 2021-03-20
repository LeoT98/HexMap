using System.IO;
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
            RefreshPosition();
            ValidateRivers();  //previene fiumi che vanno in salita

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
            return HexMetrics.colors[terrainTypeIndex];
        }
    }

    public int TerrainTypeIndex {
        get {
            return terrainTypeIndex;
        }
        set {
            if (terrainTypeIndex != value) {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }
    int terrainTypeIndex;

    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    #region FIUMI & ACQUA
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
            return  (elevation + HexMetrics.waterElevationOffset)  * HexMetrics.elevationStep;
        }
    }

    public HexDirection RiverBeginOrEndDirection {
        get {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }


    //Mare e acqua 
    public int WaterLevel {
        get {
            return waterLevel;
        }
        set {
            if (waterLevel == value) {
                return;
            }
            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }
    int waterLevel;

    public bool IsUnderwater {
        get {
            return waterLevel > elevation;
        }
    }

    public float WaterSurfaceY {
        get {
            return
                (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
        }
    }

    #endregion


    //Strade
    [SerializeField]
    bool[] roads;  //nell inspector metterlo con size=6
    [SerializeField]
    int roadMaxDeltaElevation = 2; //max differenza di altezza per cui posso mettere una strada

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

    //Features
    public int UrbanLevel {
        get {
            return urbanLevel;
        }
        set {
            if (urbanLevel != value) {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }
    int urbanLevel;

    public int FarmLevel {
        get {
            return farmLevel;
        }
        set {
            if (farmLevel != value) {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }
    int farmLevel;

    public int PlantLevel {
        get {
            return plantLevel;
        }
        set {
            if (plantLevel != value) {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    } 
    int  plantLevel;

    public int SpecialIndex {
        get {
            return specialIndex;
        }
        set {
            if (specialIndex != value && !HasRiver) {
                specialIndex = value;
                RefreshSelfOnly();
            }
        }
    }
    int specialIndex;// per le feature grosse
    public bool IsSpecial {//specialIndex == 0 indica che non c'è la special feature
        get {
            return specialIndex > 0;
        }
    }

    //Walls
    public bool Walled {
        get {
            return walled;
        }
        set {
            if (walled != value) {
                walled = value;
                Refresh();
            }
        }
    }
    bool walled;



    /////////////////////////////////////////////////////////////

    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y += // causa rumore sull'altezza
                    (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

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
        if (!IsValidRiverDestination(neighbor)) {
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
        specialIndex = 0; // il fiume toglie le special feature
        //    RefreshSelfOnly();  ci pensa la strada a refreshare
        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;  // il fiume toglie le special feature
        //  neighbor.RefreshSelfOnly();  ci pensa la strada a refreshare

        SetRoad((int)direction, false);  //si occupa anche dei refresh;
    }

    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && ( elevation >= neighbor.elevation || waterLevel == neighbor.elevation );
    }

    //rimuove fiumi nn validi
    void ValidateRivers()
    {
        if (hasOutgoingRiver &&  !IsValidRiverDestination(GetNeighbor(outgoingRiver)) ) {
            RemoveOutgoingRiver();
        }
        if (  hasIncomingRiver &&  !GetNeighbor(incomingRiver).IsValidRiverDestination(this)
        ) {
            RemoveIncomingRiver();
        }
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



    public void Save(BinaryWriter writer)// load e save devono avere le cose che coincidono come ordine e tipo
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        writer.Write(walled);

        if (hasIncomingRiver) {
            writer.Write((byte)(incomingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        if (hasOutgoingRiver) {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++) {
            if (roads[i]) {
                roadFlags |= (1 << i); // << fa bitwise shift a sinistra di i posizioni
            }
        }
        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)// load e save devono avere le cose che coincidono come ordine etipo
    {// . ReadInt32 per gli int
        terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte();
        RefreshPosition();  //sistema l'elevazione
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++) {
            roads[i] = (roadFlags & (1 << i)) != 0; // & singola fa l'AND su 1 bit
        }
    }


}