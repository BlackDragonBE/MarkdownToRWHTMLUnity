using UnityEngine;
using System.Collections;

public class SimpleRotate : MonoBehaviour
{
    public Vector3 Rotation;

    private Transform _transform;

    // Use this for initialization
    private void Awake()
    {
        _transform = transform;
    }

    // Update is called once per frame
    private void Update()
    {
        _transform.Rotate(Rotation * Time.deltaTime);
    }
}