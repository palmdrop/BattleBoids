using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassInfos : MonoBehaviour
{
    public static Boid.ClassInfo[] infos = new Boid.ClassInfo[Enum.GetNames(typeof(Boid.Type)).Length];
}
