using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualState : MonoBehaviour
{
    [Header("Render Objects")]
    public GameObject CloudRender;

    public GameObject WaterRender;

    public GameObject RainParticles;

    private Material CloudMaterial;

    private Material WaterMaterial;

    [Header("Light")]
    public Light DirectionLight;

    private Vector3 _velocity; //current velocity vector of the player, use magnitude for strength
    private float _height; //current height of the player
    private float _maxHeight; //max height allowed for player, can be used to normalize height

    private string _cloudFade = "Vector1_39F0E4CA";
    private string _waterFade = "Vector1_A4604455";
    private float _shadowMax;
    private float _rainRateOverTime;
    private float _cloudTime;

    public void Start()
    {
        CloudMaterial = CloudRender.GetComponent<MeshRenderer>().material;
        WaterMaterial = WaterRender.GetComponent<MeshRenderer>().material;

        //CloudMaterial.SetFloat(_cloudFade, 0);
        WaterMaterial.SetFloat(_waterFade, 0);
        _shadowMax = DirectionLight.shadowStrength;
        DirectionLight.shadowStrength = 0;

        if (RainParticles == null) return;
        if (!RainParticles.GetComponent<ParticleSystem>()) return;

        _rainRateOverTime = RainParticles.GetComponent<ParticleSystem>().emissionRate;

        ParticleSystem ps = GetComponent<ParticleSystem>();
        ParticleSystem.ShapeModule sh = ps.shape;
    }

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
        CloudRender.SetActive(false);

        if (RainParticles == null) return;
        if (!RainParticles.GetComponent<ParticleSystem>()) return;
        RainParticles.GetComponent<ParticleSystem>().emissionRate = 0;
    }

    //Called once when switch to Cloud state
    private void StartCloud()
    {
    }

    //Called once when switch to Steam state
    private void StartSteam()
    {
        CloudRender.SetActive(true);
        WaterRender.SetActive(false);
    }

    //Called once when switch to Rain state
    private void StartRain()
    {
        WaterRender.SetActive(true);
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
        if (WaterMaterial.GetFloat(_waterFade) == 1) return;

        //fade in water material
        float fade = Mathf.Lerp(WaterMaterial.GetFloat(_waterFade), 1, Time.deltaTime);
        WaterMaterial.SetFloat(_waterFade, fade);

        //fade in light shadow
        if (DirectionLight.shadowStrength >= _shadowMax) return;
        float shadow = Mathf.Lerp(DirectionLight.shadowStrength, _shadowMax, Time.deltaTime);
        DirectionLight.shadowStrength = shadow;
    }

    //Called once per frame during Cloud state
    private void UpdateCloud()
    {
        if (RainParticles == null) return;
        if (!RainParticles.GetComponent<ParticleSystem>()) return;

        ParticleSystem ps = GetComponent<ParticleSystem>();
        ParticleSystem.ShapeModule sh = ps.shape;

        if (RainParticles.GetComponent<ParticleSystem>().emissionRate == _rainRateOverTime) return;

        //fade out water material
        float rate = Mathf.Lerp(RainParticles.GetComponent<ParticleSystem>().emissionRate, _rainRateOverTime, Time.deltaTime / _cloudTime);
        RainParticles.GetComponent<ParticleSystem>().emissionRate = rate;
    }

    //Called once per frame during Steam state
    private void UpdateSteam()
    {
        CloudMaterial.SetFloat(_cloudFade, _height / _maxHeight);
    }

    //Called once per frame during Rain state
    private void UpdateRain()
    {
        if (RainParticles != null)
        {
            if (RainParticles.GetComponent<ParticleSystem>())
            {
                //fade out water material
                float rate = Mathf.Lerp(RainParticles.GetComponent<ParticleSystem>().emissionRate, 0, Time.deltaTime / 2);
                RainParticles.GetComponent<ParticleSystem>().emissionRate = rate;
            }
        }

        if (CloudMaterial.GetFloat(_cloudFade) == 0) return;

        //fade out water material
        float fade = Mathf.Lerp(CloudMaterial.GetFloat(_cloudFade), 0, Time.deltaTime / 2);
        CloudMaterial.SetFloat(_cloudFade, fade);
    }

    //Called once per frame during Float state
    private void UpdateFloat()
    {
        if (WaterMaterial.GetFloat(_waterFade) == 0) return;

        //fade out water material
        float fade = Mathf.Lerp(WaterMaterial.GetFloat(_waterFade), 0, Time.deltaTime);
        WaterMaterial.SetFloat(_waterFade, fade);

        if (DirectionLight.shadowStrength <= 0) return;
        float shadow = Mathf.Lerp(DirectionLight.shadowStrength, 0, Time.deltaTime);
        DirectionLight.shadowStrength = shadow;
    }

    public void SetMaxHeight(float height)
    {
        _maxHeight = height;
    }

    public void SetCloudTime(float time)
    {
        _cloudTime = time;
    }
}