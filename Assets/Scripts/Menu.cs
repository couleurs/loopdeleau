using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class Menu : MonoBehaviour
{
    public Image Icon;
    public UnityEngine.Rendering.VolumeProfile profile;
    public AudioSource AudioSource;

    public float PauseRate = 0.1f;

    private bool _paused = true;
    private bool _pauseSequenceFinish = true;

    private float _iconAlpha;
    private float _focusDistance = 13;
    private UnityEngine.Rendering.Universal.DepthOfField _dof;

    private void Start()
    {
        //set UI size
        int size = (int)(Screen.height / 1.325f);
        Icon.rectTransform.sizeDelta = new Vector2(size, size);
        _iconAlpha = Icon.color.a; //get alpha

        //get dof
        if (!profile.TryGet(out _dof)) return;

        //start the game in a paused state
        _dof.focusDistance.Override(0);
        Time.timeScale = 0;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.P))
        {
            _paused = !_paused;
            _pauseSequenceFinish = false;
            AudioSource.Play();
        }

        if (_pauseSequenceFinish) return;

        //pause game start
        if (_paused)
        {
            //guard statement if paused
            if (_dof.focusDistance.GetValue<float>() == 0) { Debug.Log("M"); _pauseSequenceFinish = true; return; }

            //pause time
            Time.timeScale = 0;

            //set icon opacity
            float alpha = Mathf.Lerp(Icon.color.a, _iconAlpha, PauseRate);
            Color color = Icon.color;
            color.a = alpha;
            Icon.color = color;

            //set blur
            float distance = Mathf.Lerp(_dof.focusDistance.GetValue<float>(), 0, PauseRate);
            _dof.focusDistance.Override(distance);
        }

        //unpause game
        if (!_paused)
        {
            Debug.Log(_dof.focusDistance.GetValue<float>());
            //guard statement if paused
            if (_dof.focusDistance.GetValue<float>() >= _focusDistance) { Debug.Log("S"); _pauseSequenceFinish = true; return; }

            //pause time
            Time.timeScale = 1;

            //set icon opacity
            float alpha = Mathf.Lerp(Icon.color.a, 0, PauseRate);
            Color color = Icon.color;
            color.a = alpha;
            Icon.color = color;

            //set blur
            float distance = Mathf.Lerp(_dof.focusDistance.GetValue<float>(), _focusDistance, PauseRate);
            _dof.focusDistance.Override(distance);
        }
    }
}