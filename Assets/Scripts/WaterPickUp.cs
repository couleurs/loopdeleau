using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPickUp : MonoBehaviour
{
    private bool _pickedUp = false;
    private int _index = 0;

    public void SetIndex(int i)
    {
        _index = i;
    }

    public int GetIndex()
    {
        return _index;
    }

    public bool PickUpStatus()
    {
        return _pickedUp;
    }

    public void ResetPickUp()
    {
        _pickedUp = false;
        gameObject.SetActive(true);
    }

    public void PickUp()
    {
        _pickedUp = true;
        gameObject.SetActive(false);
    }
}