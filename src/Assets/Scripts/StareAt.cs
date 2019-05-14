using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StareAt : MonoBehaviour
{
    public Vector3 Position;

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(Position);
    }
}
