using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public enum PlayerState { WATER, STEAM, CLOUD, RAIN }

    public PlayerState State;

    [Tooltip("This is the gameobject the camera follows")]
    public GameObject CameraPivot;

    [Tooltip("Leave this object null if you don't want the render transform to be set by the player script")]
    public GameObject Render;

    [Header("Controls")]
    public float SlopeVelocity = 1.0f;

    public float GravityVelocity = 1.0f;
    public float InputVelocity = 1.0f;
    public float WindVelocity = 1.0f;

    [Header("Transitions")]
    [Tooltip("This is how long it will take the player to go from the ground to the sky")]
    public float EvaporationTime = 5.0f;

    [Tooltip("This is how long it will take the player to go from the sky to the ground")]
    public float RainTime = 2.0f;

    [Header("")]

    //private variables
    private float _horizontalInput;

    private float _cloudHeight; //it is assumed that the height the player starts in is the Cloud height
    private int _screenWidth;
    private Vector3 _direction;
    private Rigidbody _rigidBody;
    private Vector3 _horizontalVector;
    private Vector3 _transitionPosition = Vector3.zero;
    private float _transitionTime = 0;

    // Start is called before the first frame update
    private void Start()
    {
        _screenWidth = Screen.width;
        _rigidBody = GetComponent<Rigidbody>();
        State = PlayerState.RAIN;
        _cloudHeight = transform.position.y;
    }

    // Update is called once per frame
    private void Update()
    {
        switch (State)
        {
            case PlayerState.WATER:
                WaterUpdate();
                break;

            case PlayerState.CLOUD:
                CloudUpdate();
                break;
        }

        //get horizontal input information and align it with camera direction
        _horizontalInput = GetHorizontalInput();
        if (_horizontalInput != 0)
        {
            _horizontalVector = new Vector3(_horizontalInput, 0, 0);
            _horizontalVector = Camera.main.transform.TransformDirection(_horizontalVector);
            _horizontalVector.y = 0;
        }

        //change pivot direction, and render direction for camera and rendering coordination
        if (CameraPivot != null && _direction != Vector3.zero) { CameraPivot.transform.forward = _direction; }
        if (Render != null && _rigidBody.velocity != Vector3.zero) { Render.transform.forward = _rigidBody.velocity; }
    }

    private void WaterUpdate()
    {
        _direction = DownHillDirection();
    }

    private void CloudUpdate()
    {
        _direction = WindDirection();

        //check how close the player is to the peak and switch positions when they reach the peak
        if (Vector3.Distance(transform.position, new Vector3(0, _cloudHeight, 0)) > 50) return; //TODO: this should be something other than distance, cloud runs out of power
        //TODO: maybe the cloud drops rain and this is how you get water back? and when it runs out, the cloud is done
        State = PlayerState.RAIN;
    }

    //All physics happen in the fixed update
    private void FixedUpdate()
    {
        switch (State)
        {
            case PlayerState.WATER:
                FixedWater();
                break;

            case PlayerState.CLOUD:
                FixedCloud();
                break;

            case PlayerState.STEAM:
                FixedSteam();
                break;

            case PlayerState.RAIN:
                FixedRain();
                break;
        }
    }

    private void FixedWater()
    {
        //apply downhill vector
        if (_direction != Vector3.zero)
        {
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
            _rigidBody.velocity += _horizontalVector * Time.deltaTime * InputVelocity;
            _rigidBody.velocity += CameraPivot.transform.forward * Time.deltaTime * -.1f; //this slows down the forward speed a bit
        }
    }

    private void FixedCloud()
    {
        //apply wind
        _rigidBody.AddForce(_direction * WindVelocity, ForceMode.Acceleration);
        Debug.Log(_direction);
        //apply horizontal input force
        if (_horizontalInput != 0)
        {
            _rigidBody.velocity += _horizontalVector * Time.deltaTime * InputVelocity;
            //_rigidBody.velocity += CameraPivot.transform.forward * Time.deltaTime * -.1f; //this slows down the forward speed a bit
            //TODO: make it so that the backpull is actually an animation curve so
            //TODO: make camera not clip through slope
            //TODO: add pick up objects and grow sphere
            //TODO: make camera micro to macro as growth happens
        }
    }

    private void FixedSteam()
    {
        //check the train right below the player
        if (_transitionPosition == Vector3.zero) _transitionPosition = transform.position;

        //move object towards ground
        _transitionTime += Time.deltaTime;
        Vector3 cloudPosition = new Vector3(_transitionPosition.x, _cloudHeight, _transitionPosition.z);
        Vector3 move = Vector3.Lerp(_transitionPosition, cloudPosition, _transitionTime / EvaporationTime);
        _rigidBody.MovePosition(move);

        //check how far we are away from the gounrd
        if (transform.position.y < _cloudHeight - transform.localScale.y / 2) return;

        //once we have reached the ground, we need to transition out of the rain state and into the water state
        State = PlayerState.CLOUD;
        _transitionTime = 0;
        _transitionPosition = Vector3.zero;
        _rigidBody.velocity = Vector3.zero;
    }

    private void FixedRain()
    {
        //check the train right below the player
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, Vector3.down, out hit)) return; //if nothing is hit we return zero
        if (hit.transform.gameObject.tag != "Terrain") return;
        if (_transitionPosition == Vector3.zero) _transitionPosition = transform.position;

        //move object towards ground
        _transitionTime += Time.deltaTime;
        Vector3 move = Vector3.Lerp(_transitionPosition, hit.point, _transitionTime / RainTime);
        _rigidBody.MovePosition(move);

        //check how far we are away from the gounrd
        if (hit.distance > transform.localScale.y / 2) return;

        //once we have reached the ground, we need to transition out of the rain state and into the water state
        State = PlayerState.WATER;
        _transitionTime = 0;
        _transitionPosition = Vector3.zero;
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
    /// This also checks for when to change the player state from water to steam
    /// </summary>
    /// <returns></returns>
    private Vector3 DownHillDirection()
    {
        Vector3 direction = Vector3.zero;

        RaycastHit hit;
        if (!Physics.Raycast(transform.position, Vector3.down, out hit)) return direction; //if nothing is hit we return zero

        if (hit.transform.gameObject.name == "Ocean") { State = PlayerState.STEAM; return direction; }

        if (hit.transform.gameObject.tag != "Terrain") return direction;

        if (hit.distance > transform.localScale.y / 2) return direction;

        Vector3 normal = hit.normal;
        Vector3 downhill = Vector3.ProjectOnPlane(normal, Vector3.up);
        downhill.Normalize();
        direction = downhill;
        Debug.DrawRay(transform.position, normal, Color.red);

        return direction;
    }

    /// <summary>
    /// Direction of the wind, assumes the environment's tallest point is at 0,0,0 and gets the direction towards that
    /// </summary>
    /// <returns></returns>
    private Vector3 WindDirection()
    {
        Vector3 wind = -transform.position;
        wind.y = 0;
        wind.Normalize();
        return wind;
    }
}