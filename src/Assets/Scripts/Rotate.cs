using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Vector3 Center;
    public float speed;

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(Center, Vector3.up, speed * Time.deltaTime);
    }
}
