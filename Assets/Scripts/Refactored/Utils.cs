using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct TailSection
{
    public Vector2 pos;
    public Vector2 vel;

    //To write RopeSection.zero
    public static readonly TailSection zero = new TailSection(Vector2.zero);

    public TailSection(Vector2 pos)
    {
        this.pos = pos;

        this.vel = Vector2.zero;
    }
}