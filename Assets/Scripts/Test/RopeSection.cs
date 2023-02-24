using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct RopeSection
{
    public Vector2 pos;
    public Vector2 vel;

    //To write RopeSection.zero
    public static readonly RopeSection zero = new RopeSection(Vector3.zero);

    public RopeSection(Vector3 pos)
    {
        this.pos = pos;

        this.vel = Vector3.zero;
    }
}