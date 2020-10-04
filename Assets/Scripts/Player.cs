using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public enum PlayerState { WATER, STEAM, CLOUD, RAIN, FLOAT }

    public PlayerState State;

    [Header("Objects")]
    [Tooltip("This is the gameobject the camera follows")]
    public GameObject CameraPivot;

    [Tooltip("Leave this object null if you don't want the render transform to be set by the player script")]
    public GameObject Render;

    [Tooltip("GameObject representing at which height the clouds will be")]
    public GameObject CloudLevel;

    [Header("Controls")]
    [Tooltip("Velocity multiplier for input horizontal movement")]
    public float InputVelocity = 1.0f;

    [Tooltip("Velocity multiplier for input forward/back movement")]
    public AnimationCurve ForwardVelocity;

    [Tooltip("Each RainRate interval in seconds, the cloud will drop water, once all water is dropped, it transitions to rain")]
    public float RainRate = 3.0f;

    [Header("Environment Velocity")]
    [Tooltip("Velocity multiplier when on a sloped surface")]
    public float SlopeVelocity = 1.0f;

    [Tooltip("Velocity multiplier when in the air")]
    public float GravityVelocity = 1.0f;

    [Tooltip("Velocity multiplier for wind")]
    public float WindVelocity = 1.0f;

    [Header("Transitions")]
    [Tooltip("This is how long it will take the player to go from the ground to the sky")]
    public float EvaporationTime = 10.0f;

    [Tooltip("When the player hits the ocean, the amount of time they float out")]
    public float FloatTime = 5.0f;

    [Tooltip("This is how long it will take the player to go from the sky to the ground")]
    public float RainTime = 2.0f;

    [Header("Raycast Layers")]
    public LayerMask RaycastLayers;

    //private variables
    private float _horizontalInput;

    private float _verticalInput;
    private VisualState _visualState;
    private float _cloudHeight; //it is assumed that the height the player starts in is the Cloud height
    private int _screenWidth;
    private Vector3 _direction;
    private Rigidbody _rigidBody;
    private Vector3 _horizontalVector;
    private Vector3 _transitionPosition = Vector3.zero;
    private float _transitionTime = 0;

    private void Awake()
    {
        //set cloud level height
        if (CloudLevel == null)
        {
            _cloudHeight = transform.position.y;
        }
        else
        {
            _cloudHeight = CloudLevel.transform.position.y;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        _screenWidth = Screen.width;
        _rigidBody = GetComponent<Rigidbody>();
        State = PlayerState.RAIN;

        if (GetComponent<VisualState>())
        {
            _visualState = GetComponent<VisualState>();
            _visualState.SetMaxHeight(_cloudHeight);
            _visualState.StartVisualStates(State);
        }
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

        //get horizontal and vertical/forward input information and align it with camera direction
        _horizontalInput = GetHorizontalInput();
        _verticalInput = GetVerticalInput(_horizontalInput);
        if (_horizontalInput != 0)
        {
            _horizontalVector = new Vector3(_horizontalInput, 0, 0);
            _horizontalVector = Camera.main.transform.TransformDirection(_horizontalVector);
            _horizontalVector.y = 0;
        }

        //change pivot direction, and render direction for camera and rendering coordination
        if (CameraPivot != null && _direction != Vector3.zero) { CameraPivot.transform.forward = _direction; }
        if (Render != null && _rigidBody.velocity != Vector3.zero) { Render.transform.forward = _rigidBody.velocity; }
        if (_visualState != null) { _visualState.UpdateVisualStates(State, _rigidBody.velocity, transform.position.y); }
    }

    private void WaterUpdate()
    {
        _direction = DownHillDirection();
    }

    private void CloudUpdate()
    {
        _direction = WindDirection();
        _transitionTime += Time.deltaTime;
        if (_transitionTime < RainRate) return;

        if (GetComponent<WaterManager>() == null) return;

        GetComponent<WaterManager>().DropWater();
        _transitionTime = 0;
    }

    public void ExitCloud()
    {
        _transitionTime = 0;
        State = PlayerState.RAIN;
        if (_visualState != null) { _visualState.StartVisualStates(State); }
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

            case PlayerState.FLOAT:
                FixedFloat();
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
            _rigidBody.velocity += CameraPivot.transform.forward * Time.deltaTime * _verticalInput; //this adjusts the forward speed a bit
        }
    }

    private void FixedCloud()
    {
        //apply wind
        _rigidBody.AddForce(_direction * WindVelocity, ForceMode.Acceleration);

        //apply horizontal input force
        if (_horizontalInput != 0)
        {
            _rigidBody.velocity += _horizontalVector * Time.deltaTime * InputVelocity;
            _rigidBody.velocity += CameraPivot.transform.forward * Time.deltaTime * _verticalInput; //this adjusts the forward speed a bit
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
        Vector3 move = Vector3.Slerp(_transitionPosition, cloudPosition, _transitionTime / EvaporationTime);
        _rigidBody.MovePosition(move);

        //check how far we are away from the gounrd
        if (transform.position.y < _cloudHeight - transform.localScale.y / 2) return;

        //once we have reached the ground, we need to transition out of the rain state and into the water state
        State = PlayerState.CLOUD;
        if (_visualState != null) { _visualState.StartVisualStates(State); }
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
        if (_visualState != null) { _visualState.StartVisualStates(State); }
        _transitionTime = 0;
        _transitionPosition = Vector3.zero;
    }

    private void FixedFloat()
    {
        //if the transition vector hasn't been set, we set it with the current velocity
        if (_transitionPosition == Vector3.zero) { _transitionPosition = _rigidBody.velocity / 3; _transitionPosition.y = 0; }

        //TODO: add sine bouyancy

        //move object towards ground
        _transitionTime += Time.deltaTime;
        _rigidBody.MovePosition(transform.position + (_transitionPosition * Time.deltaTime));

        //check how far we are away from the gounrd
        if (_transitionTime < FloatTime) return;

        //once we have reached the ground, we need to transition out of the rain state and into the water state
        State = PlayerState.STEAM;
        if (_visualState != null) { _visualState.StartVisualStates(State); }
        _transitionTime = 0;
        _transitionPosition = Vector3.zero;
        _rigidBody.velocity = Vector3.zero;
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
    /// Using an animation curve we turn the float into another float
    /// This allows us to control the vertical input with the horizontal input
    /// </summary>
    /// <param name="t">float to evalute along curve</param>
    /// <returns></returns>
    private float GetVerticalInput(float t)
    {
        float v = 0;
        t = Mathf.Abs(t); //we assume the incoming float t can be both negative and positive and want to mirror our animation curve results
        v = ForwardVelocity.Evaluate(t);
        return v;
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
        if (!Physics.Raycast(transform.position, Vector3.down, out hit, RaycastLayers)) return direction; //if nothing is hit we return zero

        if (hit.transform.gameObject.name == "Ocean") { State = PlayerState.FLOAT; if (_visualState != null) { _visualState.StartVisualStates(State); } return direction; }

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

    public float GetMaxHeight()
    {
        return _cloudHeight;
    }
}