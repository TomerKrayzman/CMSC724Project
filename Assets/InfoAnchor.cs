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
        

        // print("yay");
        // print(CommonStuff.getPathString(,"ANCHOR PATH: "));
    }

    
}
