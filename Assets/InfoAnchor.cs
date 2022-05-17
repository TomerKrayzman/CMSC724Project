using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
public class InfoAnchor : MonoBehaviour
{
    XRExploration x;
    public List<GameObject> bvhPath;
    // public LayerMask BVHLayer;
    // Start is called before the first frame update
    void Start()
    {
        x=GameObject.FindObjectOfType<XRExploration>();
        this.gameObject.name=this.gameObject.transform.Find("mainBG").Find("title").GetComponent<TextMeshPro>().text;
        // bvhPath=x.getObjectCurrentBVHPath(this.gameObject,x.BVHLayer);
        // this.gameObject.transform.parent=bvhPath[0].transform;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.rotation=Quaternion.LookRotation(Vector3.Scale(Camera.main.transform.position-this.transform.position, new Vector3(1,0,1)), Vector3.up);

        // print("yay");
        // print(CommonStuff.getPathString(,"ANCHOR PATH: "));
    }

    
}
