using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

#nullable enable
public abstract class EntityBrain : MonoBehaviour
{
    public abstract EntityMove GetNextMove(CellEntity entity);

    public MoveFlags GetMoveFlags(CellEntity entity, Surface? surface)
    {
        MoveFlags flags = MoveFlags.None;
        if (surface != null)
        {
            // handle death upon falling and touching fatal surfaces
            if (
                (surface.HasFlag(Surface.Flags.Fatal)) ||
                (entity.Falling && !surface.HasFlag(Surface.Flags.SuppressFall))
            )
            {
                flags |= MoveFlags.Fatal;
            }
        }
        return flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityMove? GetForwardMove(CellEntity entity, MoveFlags flags = MoveFlags.None, float maxFloorSnapAngle = 60f)
    {
        return GetMoveFromMotion(entity.transform.forward, entity, flags, maxFloorSnapAngle);
    }

    public EntityMove? GetMoveFromMotion(Vector2 motion, CellEntity entity, MoveFlags flags = MoveFlags.None, float maxFloorSnapAngle = 60f)
    {
        Move move = new(
            entity.Center + (Vector3)motion * entity.World.GridScale,
            entity.transform.rotation,
            flags,
            0.5f,
            maxFloorSnapAngle
        );

        return move.Process(entity);
    }
}
