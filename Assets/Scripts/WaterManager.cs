using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterManager : MonoBehaviour
{
    [Header("Water Pick Ups")]
    [Tooltip("Number of waters picked up")]
    public int WaterBalance;

    public LayerMask RaycastLayers;

    [Header("Water Pick Ups")]
    [Tooltip("Water Pick Up prefab")]
    public GameObject WaterPickUpPrefab;

    [Tooltip("Starting scale for both player controler and water pick ups")]
    public float StartScale = 0.1f;

    [Tooltip("The number of available pick ups in the scene")]
    public int PickUpPopulation = 200;

    [Header("Scale")]
    public int XMin = 0;

    public int XMax = 0;
    public int ZMin = 0;
    public int ZMax = 0;

    //private variables
    private GameObject[] _pickUps;

    private int _instantiateIndex = 0;
    private List<int> _pickUpIndices;
    private float Y = 1000;
    private Vector3 _scale;

    // Start is called before the first frame update
    private void Start()
    {
        _pickUps = new GameObject[PickUpPopulation];
        _pickUpIndices = new List<int>();
        //set up instantiation height

        if (!GetComponent<Player>()) return;

        Y = GetComponent<Player>().GetMaxHeight();
        _scale = new Vector3(StartScale, StartScale, StartScale);
        GetComponent<Player>().gameObject.transform.localScale = _scale;
    }

    // Update is called once per frame
    private void Update()
    {
        //scale the player after pickups
        if (transform.localScale != _scale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _scale, Time.deltaTime / 2);
        }

        //while the list isn't full we instantiate pickups
        if (_instantiateIndex >= PickUpPopulation) return;
        GameObject pickUp = InstiantiatePickUp();
        if (pickUp == null) return;
        pickUp.GetComponent<WaterPickUp>().SetIndex(_instantiateIndex);
        _pickUps[_instantiateIndex] = pickUp;
        _instantiateIndex++;
    }

    public GameObject InstiantiatePickUp()
    {
        GameObject go = null;

        while (go == null)
        {
            Vector3 rayStart = new Vector3(Random.Range(XMin, XMax), Y, Random.Range(ZMin, ZMax));

            RaycastHit hit;
            if (!Physics.Raycast(rayStart, Vector3.down, out hit, RaycastLayers)) return null; //if nothing is hit we return zero

            if (hit.transform.gameObject.tag != "Terrain") return null;

            //TODO: only instantiate on not slopes

            Vector3 position = hit.point;
            GameObject pickup = Instantiate(WaterPickUpPrefab, position, Quaternion.identity, null);
            pickup.transform.localScale = new Vector3(StartScale, StartScale, StartScale);
            go = pickup;
        }

        return go;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if it isn't a pick up, we don't care
        if (collision.gameObject.tag != "PickUp") return;
        //if already picked up, don't do anything
        WaterPickUp pickup = collision.gameObject.GetComponent<WaterPickUp>();
        if (pickup.PickUpStatus()) return;

        //pick up droplet
        _pickUpIndices.Add(pickup.GetIndex());
        pickup.PickUp();
        WaterBalance++;

        //increase the scale of the object
        _scale.x += StartScale;
        _scale.y += StartScale;
        _scale.z += StartScale;
    }

    public void DropWater()
    {
        //test if below cloud is appropriate drop position (i.e. it is a piece of terrain)
        Vector3 rayStart = transform.position; //Add ignore masks for future only testing terrain
        rayStart.y = Y;

        RaycastHit hit;
        if (!Physics.Raycast(rayStart, Vector3.down, out hit, RaycastLayers)) return; //if nothing is hit we return zero

        if (hit.transform.gameObject.tag != "Terrain") return; //if it isn't a terrain we also return

        //TODO: create instantiation point checker

        //recycling pick ups
        GameObject pickUp = _pickUps[_pickUpIndices[0]];
        _pickUpIndices.RemoveAt(0); //remove this object from the pickup list
        pickUp.transform.position = hit.point; //move the pickup to a position below the player
        pickUp.GetComponent<WaterPickUp>().ResetPickUp(); //reset the component
        WaterBalance--;

        //decrease the scale of the object
        _scale.x -= StartScale;
        _scale.y -= StartScale;
        _scale.z -= StartScale;

        //if we are out of water, we switch out of cloud
        if (_pickUpIndices.Count == 0) { GetComponent<Player>().ExitCloud(); return; }
    }
}