using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public enum GroundTruthMethods{CurrentRoom,CurrentRoomAndNeighbors, Visibility,VisibilityAndScreenSpace}
public enum DataBaseMethod{CurrentRoom,CurrentRoomAndNeighbors, Visibility,VisibilityAndScreenSpace,Euclidean}


public class XRExploration : MonoBehaviour
{
    [SerializeField]
    public int BVHLayer;
    public List<GameObject> currentPath;
    public InfoAnchor[] anchors;

    // public float maxDistanceFromAnchors=2.0f;

    // public bool sortAnchors=false;

    List<InfoAnchor> results;
    List<InfoAnchor> groundTruthAnchors;

    public int numNeighbors=0;

    GameObject[] cameraPoints;
    int currentCamPoint=0;

    float totalAccuracy=0.0f;
    int totalValidPoints=0;
    public float radius=1.0f;
    public GroundTruthMethods gtMethod;
    public DataBaseMethod dbMethod;
    public bool sampleVisOrSort=false;

    public bool dataCollect=false;

    public bool trackBVH=true;
    int currentFrame=0;
    int totalNodesTouched=0;

    public GameObject samplePointPrefab;

    List<GameObject> samplePoints;
    //mapping of user positions>

    public Vector3 sampleDensity;

    BVHNode[] allNodes;

    // Start is called before the first frame update
    void Start()
    {
        allNodes=GameObject.FindObjectsOfType<BVHNode>();
        //hide all anchors
        anchors=GameObject.FindObjectsOfType<InfoAnchor>();
        print("this many anchors: "+anchors.Length);
        results=new List<InfoAnchor>();
        groundTruthAnchors=new List<InfoAnchor>();
        samplePoints= new List<GameObject>();

        if ((dbMethod==DataBaseMethod.Euclidean ||dbMethod==DataBaseMethod.CurrentRoom||dbMethod==DataBaseMethod.CurrentRoomAndNeighbors)&& sampleVisOrSort){
            //spawn a bunch of sample points at specific interval in x,y,z. x:-10 to 10. z -10 to 10. y too to 5
            //for each point, sort all anchors by distance
            foreach (BVHNode b in allNodes){
                Vector3 min=b.gameObject.GetComponent<Collider>().bounds.min;
                Vector3 max=b.gameObject.GetComponent<Collider>().bounds.max;
                Vector3 extents=max-min;
                float xStep=Mathf.Abs(extents.x)/sampleDensity.x;
                float yStep=Mathf.Abs(extents.y)/sampleDensity.y;
                float zStep=Mathf.Abs(extents.z)/sampleDensity.z;
                print("steps: "+(extents.x/xStep)+", "+(extents.y/yStep)+", "+(extents.z/zStep));
                int totalNum=0;
                for (float x=min.x;x<=max.x;x+=xStep){
                    for (float z=min.z;z<=max.z;z+=zStep){
                        for (float y=min.y;y<=max.y;y+=yStep){
                            GameObject newSamplePt=Instantiate(samplePointPrefab,new Vector3(x,y,z),Quaternion.identity);
                            samplePoints.Add(newSamplePt);
                            totalNum++;
                            
                            if (dbMethod==DataBaseMethod.Euclidean){
                                newSamplePt.GetComponent<SamplePoint>().anchorsSorted=anchors.OrderBy(
                                    a => Vector3.Distance(newSamplePt.transform.position,a.transform.position)
                                ).ToList();
                            } else if (dbMethod==DataBaseMethod.CurrentRoom || dbMethod==DataBaseMethod.CurrentRoomAndNeighbors){
                                newSamplePt.GetComponent<SamplePoint>().anchorsVisible=new List<InfoAnchor>();
                                //find all nodes visible from here
                                RaycastHit[] hits;
                                foreach(InfoAnchor i in anchors){         
                                    bool hitSomething=false;   
                                    hits=Physics.RaycastAll(newSamplePt.transform.position,i.transform.position-newSamplePt.transform.position, (i.transform.position-newSamplePt.transform.position).magnitude, 1<<0);
                                    if (hits.Length>0){
                                        foreach(RaycastHit r in hits){
                                            if (r.collider.gameObject.layer==0){
                                                hitSomething=true;
                                                break;
                                            }
                                        }
                                    }
                                    if (hitSomething){
                                        // i.gameObject.SetActive(false);
                                    } else{
                                        // i.gameObject.SetActive(true);
                                        // groundTruthAnchors.Add(i);
                                        
                                        newSamplePt.GetComponent<SamplePoint>().anchorsVisible.Add(i);
                                    }
                                }
                            }
                            
                            
                        }
                    }   
                }
                print("total num="+totalNum);
            }
            
        }
        // cameraPoints=new List<GameObject>();
        cameraPoints=GameObject.FindGameObjectsWithTag("CameraPoint");
    }

    // Update is called once per frame
    void Update()
    {
        if (dataCollect){
            if (currentCamPoint==cameraPoints.Length){
                print("-------------final average="+(totalAccuracy/totalValidPoints)+", avg nodes touched: "+(totalNodesTouched/(currentCamPoint+1)));
                Debug.Break();
            }
            Camera.main.gameObject.transform.position=cameraPoints[currentCamPoint].transform.position;
            currentCamPoint++;
        }

        /*
        foreach(InfoAnchor i in anchors){
            i.gameObject.transform.rotation=Quaternion.LookRotation(Vector3.Scale(Camera.main.transform.position-i.gameObject.transform.position, new Vector3(1,0,1)), Vector3.up);
        }*/


        // print(Camera.main.gameObject.name);
        // print(""+BVHLayer);
        currentPath=this.getObjectCurrentBVHPath(Camera.main.gameObject,BVHLayer);
        // print(this.getPathString(currentPath,"CAMERA PATH: "));
        disableAllAnchors();
        //search for the anchors the user should see now by either
        //1. euclidean distance;no bvh. maybe index?
        //2. bvh but not tracking where user is; start at the root
        //3. bvh but starting at user's current node
        // enableAnchorsWithEuclideanDistanceNotIndexed(maxDistanceFromAnchors);
        // enableAnchorsInSameRoom(false,false);
        // enableAnchorsInSameRoomAndNeighbor(false,false);
        if (gtMethod==GroundTruthMethods.CurrentRoom){
            groundTruthForUserCurrentRoom();
        } else if (gtMethod==GroundTruthMethods.CurrentRoomAndNeighbors){
            groundTruthForUserCurrentRoomAndNeighbors();
        } else if (gtMethod==GroundTruthMethods.Visibility){
            groundTruthForVisibility();
        } else if (gtMethod==GroundTruthMethods.VisibilityAndScreenSpace){
            groundTruthForVisibilityAndScreenSpace();
        }

        if (dbMethod==DataBaseMethod.CurrentRoom){
            totalNodesTouched+=enableAnchorsInSameRoom(trackBVH,sampleVisOrSort);
        } else if (dbMethod==DataBaseMethod.CurrentRoomAndNeighbors){
            totalNodesTouched+=enableAnchorsInSameRoomAndNeighbor(trackBVH,sampleVisOrSort);
        } else if (dbMethod==DataBaseMethod.Euclidean){
            totalNodesTouched+=enableAnchorsWithEuclideanDistance(radius,sampleVisOrSort);
        } else if (dbMethod==DataBaseMethod.Visibility){
            totalNodesTouched+=enableAnchorsWithVisibility();
        }  else if (dbMethod==DataBaseMethod.VisibilityAndScreenSpace){
            totalNodesTouched+=enableAnchorsWithVisibilityAndScreenSpace();
        }
        
        float percentCorrect=percentageCorrect(groundTruthAnchors,results);
        if (!float.IsNaN(percentCorrect)){
            totalAccuracy+=percentCorrect;
            totalValidPoints++;
        }
        print("results: "+percentCorrect+", avg nodes touched: "+(totalNodesTouched/(currentCamPoint+1))+", "+this.getPathString(currentPath,"CAMERA PATH: "));//+", gt="+groundTruthForVisibility().Count);

        //ground truth is how many anchors should actually be visible 

        if (Input.GetKeyDown(KeyCode.P)){
            //push current hierarchy to firebase. easier than doing manually
        }

        if (Input.GetKeyDown(KeyCode.L)){
            //shoot ray
            //get screenspace
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position,Camera.main.transform.forward,out hit, 10.0f)){
                if (hit.collider.gameObject.name=="mainBG"){
                    Vector3 screenMin = Camera.main.WorldToScreenPoint(hit.collider.bounds.min);
                    Vector3 screenMax = Camera.main.WorldToScreenPoint(hit.collider.bounds.max);
                    
                    float screenWidth = screenMax.x - screenMin.x;
                    float screenHeight = screenMax.y - screenMin.y;

                    print("w: "+Mathf.Abs(screenWidth*100.0f/Screen.width));
                }
                
                
            }
        }

        currentFrame++;

    }

    float percentageCorrect(List<InfoAnchor> groundTruth, List<InfoAnchor> results){
        int numRight=0;
        foreach (InfoAnchor i in groundTruth){
            if (results.Contains(i)){
                numRight++;
            } 
        }
        return ((float) numRight)/((float)(groundTruth.Count+results.Count-numRight));
    }

    List<InfoAnchor> groundTruthForUserCurrentRoom(){
        groundTruthAnchors.Clear();
        for (int j=0; j< currentPath[0].transform.childCount; j++){
            if (currentPath[0].transform.GetChild(j).gameObject.GetComponent<InfoAnchor>()){
                groundTruthAnchors.Add(currentPath[0].transform.GetChild(j).gameObject.GetComponent<InfoAnchor>());
            }
        }

        return groundTruthAnchors;
    }

    List<InfoAnchor> groundTruthForUserCurrentRoomAndNeighbors(){
        groundTruthAnchors.Clear();
        for (int j=0; j< currentPath[0].transform.childCount; j++){
            if (currentPath[0].transform.GetChild(j).gameObject.GetComponent<InfoAnchor>()){
                groundTruthAnchors.Add(currentPath[0].transform.GetChild(j).gameObject.GetComponent<InfoAnchor>());
            }
        }
        //get the 4 neighbors that are closest to it
        // List<GameObject> neighborBoxes=new List<GameObject>();

        // GameObject closestBox, secondClosestBox,thirdClosestBox,fourthClosestBox;

        // print("Ground truth anchors");
        foreach(BVHNode b in currentPath[0].GetComponent<BVHNode>().neighborsVisibleFromThis){
            for (int j=0; j< b.transform.childCount; j++){
                if (b.transform.GetChild(j).gameObject.GetComponent<InfoAnchor>()){
                    groundTruthAnchors.Add(b.transform.GetChild(j).gameObject.GetComponent<InfoAnchor>());
                }
            }
        }
        return groundTruthAnchors;
    }

    List<InfoAnchor> groundTruthForVisibility(){
        groundTruthAnchors.Clear();
        enableAllAnchors();
        RaycastHit[] hits;
        foreach(InfoAnchor i in anchors){         
            bool hitSomething=false;   
            hits=Physics.RaycastAll(Camera.main.transform.position,i.transform.position-Camera.main.transform.position, (i.transform.position-Camera.main.transform.position).magnitude, 1<<0);
            if (hits.Length>0){
                foreach(RaycastHit r in hits){
                    if (r.collider.gameObject.layer==0){
                        hitSomething=true;
                        break;
                    }
                }
            }
            if (hitSomething){
                // i.gameObject.SetActive(false);
            } else{
                // i.gameObject.SetActive(true);
                groundTruthAnchors.Add(i);
            }
        }
        return groundTruthAnchors;
    }

    List<InfoAnchor> groundTruthForVisibilityAndScreenSpace(){ //7.5% screen space?
        groundTruthAnchors.Clear();
        enableAllAnchors();
        RaycastHit[] hits;
        foreach(InfoAnchor i in anchors){         
            bool hitSomething=false;   
            hits=Physics.RaycastAll(Camera.main.transform.position,i.transform.position-Camera.main.transform.position, ((i.transform.position-Camera.main.transform.position)*1.01f).magnitude, 1<<0);
            if (hits.Length>0){
                foreach(RaycastHit r in hits){
                    if (r.collider.gameObject.layer==0){
                        hitSomething=true;
                        break;
                    }
                }
            }
            if (hitSomething){
                // i.gameObject.SetActive(false);
            } else{
                // i.gameObject.SetActive(true);
                
                Vector3 screenMin = Camera.main.WorldToScreenPoint(i.transform.GetChild(0).gameObject.GetComponent<Collider>().bounds.min);
                Vector3 screenMax = Camera.main.WorldToScreenPoint(i.transform.GetChild(0).gameObject.GetComponent<Collider>().bounds.max);
                
                float screenWidth = screenMax.x - screenMin.x;
                float screenHeight = screenMax.y - screenMin.y;
                if (Mathf.Abs(screenWidth*100.0f/Screen.width)>0.075){
                    groundTruthAnchors.Add(i);
                }
                // print("w: "+);
            }
        }
        return groundTruthAnchors;
    }

    int enableAnchorsWithVisibilityAndScreenSpace(){
        results.Clear();
        enableAllAnchors();
        int nodesTouched=0;
        RaycastHit[] hits;
        foreach(InfoAnchor i in anchors){        
            nodesTouched++;    
            bool hitSomething=false;   
            hits=Physics.RaycastAll(Camera.main.transform.position,i.transform.position-Camera.main.transform.position, (i.transform.position-Camera.main.transform.position).magnitude, 1<<0);
            if (hits.Length>0){
                foreach(RaycastHit r in hits){
                    if (r.collider.gameObject.layer==0){
                        hitSomething=true;
                        break;
                    }
                }
            }
            if (hitSomething){
                // i.gameObject.SetActive(false); causes collision to break
            } else{
                Vector3 screenMin = Camera.main.WorldToScreenPoint(i.transform.GetChild(0).gameObject.GetComponent<Collider>().bounds.min);
                Vector3 screenMax = Camera.main.WorldToScreenPoint(i.transform.GetChild(0).gameObject.GetComponent<Collider>().bounds.max);
                
                float screenWidth = screenMax.x - screenMin.x;
                float screenHeight = screenMax.y - screenMin.y;
                if (Mathf.Abs(screenWidth*100.0f/Screen.width)>0.075){
                    i.gameObject.SetActive(true);
                    results.Add(i);
                } else{
                    // i.gameObject.SetActive(false); causes collision to break
                }
                
            }
        }
        return nodesTouched;
    }

    int enableAnchorsWithVisibility(){
        results.Clear();
        enableAllAnchors();
        RaycastHit[] hits;
        int nodesTouched=0;
        foreach(InfoAnchor i in anchors){     
            nodesTouched++;    
            bool hitSomething=false;   
            hits=Physics.RaycastAll(Camera.main.transform.position,i.transform.position-Camera.main.transform.position, (i.transform.position-Camera.main.transform.position).magnitude, 1<<0);
            if (hits.Length>0){
                foreach(RaycastHit r in hits){
                    if (r.collider.gameObject.layer==0){
                        hitSomething=true;
                        break;
                    }
                }
            }
            if (hitSomething){
                i.gameObject.SetActive(false);
            } else{
                i.gameObject.SetActive(true);
                results.Add(i);
            }
        }
        return nodesTouched;
    }


    void disableAllAnchors(){
        foreach (InfoAnchor i in anchors){
            i.gameObject.SetActive(false);
        }
    }
    void enableAllAnchors(){
        foreach (InfoAnchor i in anchors){
            i.gameObject.SetActive(true);
        }
    }

    int enableAnchorsWithEuclideanDistance(float maxDistance, bool indexed){
        results.Clear();
        int nodesTouched=0;

        if (!indexed){
            foreach (InfoAnchor i in anchors){
                if ((i.transform.position-Camera.main.transform.position).magnitude<maxDistance){
                    i.gameObject.SetActive(true);
                    results.Add(i);
                    
                }
                nodesTouched++;
            }
        } else{
            //find nearest sample point to cam. add elements from it until distance too high
            GameObject closestSamplePoint=null;
            foreach (GameObject sam in samplePoints){
                if (closestSamplePoint==null || (sam.gameObject.transform.position-Camera.main.transform.position).magnitude<(closestSamplePoint.gameObject.transform.position-Camera.main.transform.position).magnitude){
                    closestSamplePoint=sam;
                }
            }
            int currentIndex=0;
            SamplePoint samP=closestSamplePoint.GetComponent<SamplePoint>();
            while((samP.anchorsSorted[currentIndex].gameObject.transform.position-Camera.main.transform.position).magnitude<maxDistance){
                samP.anchorsSorted[currentIndex].gameObject.SetActive(true);
                results.Add(samP.anchorsSorted[currentIndex]);
                currentIndex++;
                nodesTouched++;
            }
            nodesTouched++; //needs to touch last node to know if it's too far
        }
        return nodesTouched; //needs to check every anchor
    }

    int enableAnchorsInSameRoom(bool trackUser, bool sampleVisibility){
        results.Clear();
        int nodesTouched=0;
        //currently tracked b/c starts from user node

        if (trackUser){
            for (int j=0; j< currentPath[0].transform.childCount; j++){
                if (currentPath[0].transform.GetChild(j).gameObject.GetComponent<InfoAnchor>()){
                    currentPath[0].transform.GetChild(j).gameObject.SetActive(true);
                    results.Add(currentPath[0].transform.GetChild(j).GetComponent<InfoAnchor>());
                    nodesTouched++;
                }
            }
        } else{
            //starting from scenebvh, check each of the children until find one in the user's path
            GameObject currentNode=GameObject.Find("SceneBVH");
            nodesTouched++;
            bool userFound=false;
            GameObject userCurrentLoc=null;
            while (!userFound){
                for(int i=0;i<currentNode.transform.childCount;i++){
                    nodesTouched++;
                    if (currentPath.Contains(currentNode.transform.GetChild(i).gameObject)){
                        // userFound=true;
                        userCurrentLoc=currentNode.transform.GetChild(i).gameObject;
                        if (currentPath[0]==currentNode.transform.GetChild(i).gameObject){
                            userFound=true;
                        }
                        break; //leaves for loop
                    }
                }
                currentNode=userCurrentLoc;
            }

            for (int j=0; j< userCurrentLoc.transform.childCount; j++){
                if (userCurrentLoc.transform.GetChild(j).gameObject.GetComponent<InfoAnchor>()){
                    userCurrentLoc.transform.GetChild(j).gameObject.SetActive(true);
                    results.Add(userCurrentLoc.transform.GetChild(j).GetComponent<InfoAnchor>());
                    nodesTouched++;
                }
            }
        }

        //find nearest sample point
        //add any nodes not in the list already. the list has all anchors that are visible from that point, unsorted
        GameObject closestSamplePoint=null;
        foreach (GameObject sam in samplePoints){
            if (closestSamplePoint==null || (sam.gameObject.transform.position-Camera.main.transform.position).magnitude<(closestSamplePoint.gameObject.transform.position-Camera.main.transform.position).magnitude){
                closestSamplePoint=sam;
            }
        }

        foreach(InfoAnchor info in closestSamplePoint.GetComponent<SamplePoint>().anchorsVisible){
            if (!results.Contains(info)){
                nodesTouched++;
                results.Add(info);
            }
        }
        // foreach (InfoAnchor g in gs){
        //     g.gameObject.SetActive(true);
        //     results.Add(g);
        // }
        return nodesTouched;
    }

    int enableAnchorsInSameRoomAndNeighbor(bool trackUser, bool sampleVisibility){
        int sameRoomResult=enableAnchorsInSameRoom(trackUser, sampleVisibility);
        int nodesTouched=sameRoomResult;
        // List<InfoAnchor> gs=results;//=groundTruthForUserCurrentRoomAndNeighbors();  /////doesn't give num touched nodes
        //currently tracked b/c starts from user node
        foreach(BVHNode b in currentPath[0].GetComponent<BVHNode>().neighborsVisibleFromThis){
            for (int j=0; j< b.transform.childCount; j++){
                if (b.transform.GetChild(j).gameObject.GetComponent<InfoAnchor>()){
                    b.transform.GetChild(j).gameObject.SetActive(true);
                    results.Add(b.transform.GetChild(j).gameObject.GetComponent<InfoAnchor>());
                    nodesTouched++;
                }
            }
        }
        //find nearest sample point
        //add any nodes not in the list already. the list has all anchors that are visible from that point, unsorted
        GameObject closestSamplePoint=null;
        foreach (GameObject sam in samplePoints){
            if (closestSamplePoint==null || (sam.gameObject.transform.position-Camera.main.transform.position).magnitude<(closestSamplePoint.gameObject.transform.position-Camera.main.transform.position).magnitude){
                closestSamplePoint=sam;
            }
        }

        foreach(InfoAnchor info in closestSamplePoint.GetComponent<SamplePoint>().anchorsVisible){
            if (!results.Contains(info)){
                nodesTouched++;
                results.Add(info);
            }
        }

        // foreach (InfoAnchor g in gs){
        //     g.gameObject.SetActive(true);
        //     results.Add(g);
        // }
        return nodesTouched;
    }


    public List<GameObject> getObjectCurrentBVHPath(GameObject g, LayerMask BVHLayer){
        
        List<GameObject> returnList=new List<GameObject>();
        Collider[] hitBoxes=Physics.OverlapSphere(g.transform.position,0.1f,1<<BVHLayer,QueryTriggerInteraction.Collide);
        // 
        if (hitBoxes.Length>0){
            
            //doesn't matter which child we start at b/c this will go down to the lowest child anyway
            Transform currentChild=hitBoxes[0].gameObject.transform;
            
            Transform nextChild=null;

            if (currentChild.transform.childCount>0){
                // print("hitboxes"+hitBoxes.Length);
                // print("currentChild transform " +(currentChild.childCount));
                while (currentChild.childCount>0){
                    
                    nextChild=null;
                    for(int i=0; i<currentChild.childCount;i++){
                        if (nextChild!=null){
                            break;
                        }

                        if (currentChild.GetChild(i).gameObject.layer==6){
                            Collider[] colliders=currentChild.GetChild(i).gameObject.GetComponents<Collider>();
                            foreach (Collider c in colliders){
                                if (Array.IndexOf(hitBoxes,c)>-1){
                                    nextChild=currentChild.GetChild(i);
                                    // print("next child"+nextChild.gameObject.name);
                                    break;
                                }
                            }
                        }
                    }
                    // currentChild=currentChild.transform.GetChild(0);
                    // print("curr, next " +(currentChild.gameObject.name)+", "+(nextChild?nextChild.gameObject.name:"Null"));
                    if (nextChild==null){
                        
                        break;
                    }
                    currentChild=nextChild;
                }
            } else{
                // print("no children " +(currentChild==null));
            }
            returnList.Add(currentChild.gameObject);
            while (currentChild.parent!=null){
                returnList.Add(currentChild.parent.gameObject);
                currentChild=currentChild.parent;
            }
            // print("num children for this"+returnList.Count);
        } else{
            print("ANCHOR IS NOT INSIDE ANYTHING!");
            return null;
        }
        return returnList;
    }

    public string getPathString(List<GameObject> pathObjects, string prefix){
        string currentPathString=prefix;
        
        for (int i=0;i< pathObjects.Count;i++){
            currentPathString+=pathObjects[i].name+(i==pathObjects.Count-1?"":" < ");
        }
        return currentPathString;
    }
    
}
