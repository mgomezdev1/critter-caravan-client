using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable
[RequireComponent(typeof(CellElement))]
public class SingleSurface : MonoBehaviour
{
    private CellElement cellElement;
    [SerializeField] [Range(0, 360)] private float wallAngle;
    [SerializeField] private Vector2 wallOffset = Vector2.zero;
    [SerializeField] private bool makeWallOffsetRelative = true;
    [SerializeField] private Vector2Int cellOffset = Vector2Int.zero;

    [Header("Wall Properties")]
    [SerializeField] private Surface.Flags surfaceProperties = Surface.Flags.None;
    [SerializeField] private int surfacePriority = 0;
    [SerializeField] private List<Effector> effectors = new();

    private Surface? surface = null;
    private float Rotation => transform.rotation.eulerAngles.z;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cellElement = GetComponent<CellElement>();
        cellElement.World.RegisterSurface(GetSurface());
    }

    public Surface GetSurface(bool useCache = true)
    {
        if (useCache && surface != null)
        {
            return surface;
        }
        if (surface != null)
        {
            // ensure to avoid leaving trash surfaces registered
            cellElement.World.DeregisterSurface(surface);
        }

        float gridScale = cellElement.World.GridScale;

        Vector2Int rotatedCellOffset = MathLib.RoundVector(MathLib.RotateVector2(cellOffset, Rotation));
        Vector2 rotatedNormal = MathLib.Vector2FromAngle(Rotation + wallAngle);
        Vector2 rotatedWallOffset = MathLib.RotateVector2(wallOffset, Rotation);

        Vector3 targetPosition = transform.position + (Vector3)(rotatedWallOffset * (makeWallOffsetRelative ? gridScale / 2 : 1));
        Vector3 cellCenterPosition = cellElement.World.GetCellCenter(cellElement.Cell + rotatedCellOffset);
        surface = new Surface(cellElement.Cell + rotatedCellOffset, rotatedNormal, targetPosition - cellCenterPosition, surfaceProperties, surfacePriority)
        {
            effectors = effectors
        };

        return surface;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if (surface != null && cellElement != null)
        {
            cellElement.World.DeregisterSurface(surface);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (cellElement == null)
        {
            cellElement = GetComponent<CellElement>();
        }
        GetSurface(false).DrawGizmos(cellElement.World);
    }
}
