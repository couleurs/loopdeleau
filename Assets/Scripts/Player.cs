using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public GameObject Pivot;

    [Header("Controls")]
    public float SlopeVelocity = 1.0f;

    public float GravityVelocity = 1.0f;
    public float InputVelocity = 1.0f;

    //private variables
    private float _horizontalInput;

    private int _screenWidth;
    private Vector3 _downHill;
    private Rigidbody _rigidBody;
    private Vector3 _horizontalVector;

    // Start is called before the first frame update
    private void Start()
    {
        _screenWidth = Screen.width;
        _rigidBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void Update()
    {
        _horizontalInput = GetHorizontalInput();
        _downHill = DownHillDirection();

        if (_horizontalInput != 0)
        {
            _horizontalVector = new Vector3(_horizontalInput, 0, 0);
            _horizontalVector = Camera.main.transform.TransformDirection(_horizontalVector);
            _horizontalVector.y = 0;
            Debug.DrawRay(transform.position, _horizontalVector, Color.cyan);
        }

        //change pivot direction
        if (Pivot != null && _downHill != Vector3.zero) { Pivot.transform.forward = _downHill; }
    }

    //All physics happen in the fixed update
    private void FixedUpdate()
    {
        //apply downhill vector
        if (_downHill != Vector3.zero)
        {
            //_rigidBody.velocity += _downHill * Time.deltaTime * 10;
            //_rigidBody.AddForce(_downHill * SlopeVelocity, ForceMode.Acceleration);
            _rigidBody.AddForce(Vector3.down * SlopeVelocity, ForceMode.Acceleration);
        }
        else
        {
            //gravity
            _rigidBody.AddForce(Vector3.down * GravityVelocity, ForceMode.Acceleration);
        }

        //apply horizontal input force
        if (_horizontalInput != 0)
        {
            //_rigidBody.AddTorque(-_horizontalVector * 100);
            //_rigidBody.angularVelocity *= (1 - _horizontalInput * Time.fixedDeltaTime);
            _rigidBody.velocity += _horizontalVector * Time.deltaTime * InputVelocity;
            _rigidBody.velocity += Pivot.transform.forward * Time.deltaTime * -.1f;
        }
    }

    //custom methods

    /// <summary>
    /// Get the Horizontal Input
    /// </summary>
    /// <returns></returns>
    private float GetHorizontalInput()
    {
        float input = Input.mousePosition.x;
        //Debug.Log("input: " + input);
        if (input > _screenWidth || input < 0) { return 0; }

        input = input - (_screenWidth / 2); //center number in screen, mousePosition range is 0 -> screen width
        input = input / _screenWidth * 2; //scale input based on screen size
        //if (_horizontalInput == 0) //TODO: arrow keys are optional
        //_horizontalInput = Input.GetAxis("Horizontal");
        return input;
    }

    /// <summary>
    /// Get the slope direction going down the hill
    /// </summary>
    /// <returns></returns>
    private Vector3 DownHillDirection()
    {
        Vector3 direction = Vector3.zero;

        RaycastHit hit;
        if (!Physics.Raycast(transform.position, Vector3.down, out hit)) return direction; //if nothing is hit we return zero

        if (hit.transform.gameObject.tag != "Terrain") return direction;

        if (hit.distance > transform.localScale.y) return direction;

        Vector3 normal = hit.normal;
        Vector3 downhill = Vector3.ProjectOnPlane(normal, Vector3.up);
        downhill.Normalize();
        direction = downhill;
        Debug.DrawRay(transform.position, normal, Color.red);

        return direction;
    }
}