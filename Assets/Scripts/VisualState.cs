using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualState : MonoBehaviour
{
    private Vector3 _velocity; //current velocity vector of the player, use magnitude for strength
    private float _height; //current height of the player
    private float _maxHeight; //max height allowed for player, can be used to normalize height

    //Called once when state switched
    public void StartVisualStates(Player.PlayerState State)
    {
        switch (State)
        {
            case Player.PlayerState.WATER:
                StartWater();
                break;

            case Player.PlayerState.CLOUD:
                StartCloud();
                break;

            case Player.PlayerState.STEAM:
                StartSteam();
                break;

            case Player.PlayerState.RAIN:
                StartRain();
                break;

            case Player.PlayerState.FLOAT:
                StartFloat();
                break;
        }
    }

    //Called once when switch to Water state
    private void StartWater()
    {
    }

    //Called once when switch to Cloud state
    private void StartCloud()
    {
    }

    //Called once when switch to Steam state
    private void StartSteam()
    {
    }

    //Called once when switch to Rain state
    private void StartRain()
    {
    }

    //Called once when switch to Float state
    private void StartFloat()
    {
    }

    // Update is called once per frame
    public void UpdateVisualStates(Player.PlayerState State, Vector3 velocity, float height)
    {
        _velocity = velocity;
        _height = height;

        switch (State)
        {
            case Player.PlayerState.WATER:
                UpdateWater();
                break;

            case Player.PlayerState.CLOUD:
                UpdateCloud();
                break;

            case Player.PlayerState.STEAM:
                UpdateSteam();
                break;

            case Player.PlayerState.RAIN:
                UpdateRain();
                break;

            case Player.PlayerState.FLOAT:
                UpdateFloat();
                break;
        }
    }

    //Called once per frame during Water state
    private void UpdateWater()
    {
    }

    //Called once per frame during Cloud state
    private void UpdateCloud()
    {
    }

    //Called once per frame during Steam state
    private void UpdateSteam()
    {
    }

    //Called once per frame during Rain state
    private void UpdateRain()
    {
    }

    //Called once per frame during Float state
    private void UpdateFloat()
    {
    }

    public void SetMaxHeight(float height)
    {
        _maxHeight = height;
    }
}