using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
[InitializeOnLoad]
internal static class PlayModeTestRunner
{
    private static List<string> _logs=new List<string>();
    private static UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] _grabs;
    private static UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _cup;
    private static UnityEngine.XR.Interaction.Toolkit.XRInteractionManager _manager;
    private static UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor _hand;
    private static Unity.XR.CoreUtils.XROrigin _rig;
    private static float _start;
    private static double _deadline;
    private static int _stage;
    private static Vector3 _lift;
    private static bool _failed;
    static PlayModeTestRunner()
    {
        var state=SessionState.GetString("PlayModeTest.State","Idle");
        if(state=="WaitingForCompile")EditorApplication.delayCall+=()=>{SessionState.SetString("PlayModeTest.State","EnteringPlayMode");EditorApplication.playModeStateChanged+=Change;EditorApplication.isPlaying=true;};
        else if(state=="EnteringPlayMode"||state=="InPlayMode"){EditorApplication.playModeStateChanged+=Change;if(EditorApplication.isPlaying)Start();}
        else if(state=="Done")EditorApplication.delayCall+=Cleanup;
    }
    private static void Change(PlayModeStateChange c){if(c==PlayModeStateChange.EnteredPlayMode)Start();}
    private static void Start(){EditorApplication.playModeStateChanged-=Change;SessionState.SetString("PlayModeTest.State","InPlayMode");_deadline=EditorApplication.timeSinceStartup+20;EditorApplication.update+=Tick;}
    private static void Check(bool pass,string text){_logs.Add((pass?"PASS ":"FAIL ")+text);Debug.Log("[Test] "+_logs[_logs.Count-1]);_failed|=!pass;}
    private static void Tick()
    {
        try
        {
            if(_grabs==null)
            {
                var roots=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                _grabs=roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(true)).ToArray();
                _manager=roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>(true)).First();
                _rig=roots.SelectMany(g=>g.GetComponentsInChildren<Unity.XR.CoreUtils.XROrigin>(true)).First();
                _cup=_grabs.First(g=>g.name=="Espresso Cup 2");_start=Time.time;
                Check(_grabs.Length==23,"grabs="+_grabs.Length);return;
            }
            if(EditorApplication.timeSinceStartup>_deadline){Check(false,"Timeout stage="+_stage);Finish();return;}
            float elapsed=Time.time-_start;
            if(_stage==0&&elapsed>=3)
            {
                foreach(var g in _grabs)
                {
                    var rb=g.GetComponent<Rigidbody>();
                    Check(rb.position.y>=-.03f&&rb.position.y<1.8f&&rb.linearVelocity.magnitude<.05f,g.name+" y="+rb.position.y.ToString("F3")+" speed="+rb.linearVelocity.magnitude.ToString("F3"));
                    if(g.name.StartsWith("Saucer"))Check(Mathf.Abs(rb.position.y-.785f)<.035f,"Saucer height="+rb.position.y.ToString("F3"));
                }
                var h=new GameObject("TEST Hand");h.transform.position=_cup.attachTransform.position;h.AddComponent<Rigidbody>().isKinematic=true;var col=h.AddComponent<SphereCollider>();col.radius=.025f;col.isTrigger=true;_hand=h.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();_hand.interactionManager=_manager;
                _manager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)_hand,(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)_cup);
                Check(_cup.isSelected,"Cup selected by XR manager");_lift=_cup.transform.position+Vector3.up*.25f;h.transform.position+=Vector3.up*.25f;_stage=1;
            }
            else if(_stage==1&&elapsed>=4)
            {
                Check(Vector3.Distance(_cup.transform.position,_lift)<.12f,"Velocity tracking error="+Vector3.Distance(_cup.transform.position,_lift));
                _manager.SelectExit((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)_hand,(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)_cup);
                Check(!_cup.isSelected,"Cup released");
                var provider=_rig.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true);
                Check(provider.QueueTeleportRequest(new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest{destinationPosition=new Vector3(.2f,0,-1.4f),destinationRotation=Quaternion.identity,matchOrientation=UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.WorldSpaceUp}),"Teleport accepted");_stage=2;
            }
            else if(_stage==2&&elapsed>=6)
            {
                var delta=_rig.Camera.transform.position-new Vector3(.2f,0,-1.4f);delta.y=0;Check(delta.magnitude<.15f,"Teleport landing error="+delta.magnitude);
                Check(_cup.transform.position.y>.75f&&_cup.transform.position.y<.93f&&_cup.GetComponent<Rigidbody>().linearVelocity.magnitude<.05f,"Released cup resting y="+_cup.transform.position.y);
                Finish();
            }
        }
        catch(System.Exception e){Check(false,e.ToString());Finish();}
    }
    [System.Serializable]private class TestResult{public bool success;public string[] logs;}
    private static void Finish(){EditorApplication.update-=Tick;SessionState.SetString("PlayModeTest.Result",JsonUtility.ToJson(new TestResult{success=!_failed,logs=_logs.ToArray()}));SessionState.SetString("PlayModeTest.State","Done");EditorApplication.isPlaying=false;}
    private static void Cleanup(){var path=SessionState.GetString("PlayModeTest.ScriptPath","");SessionState.EraseString("PlayModeTest.State");SessionState.EraseString("PlayModeTest.ScriptPath");if(!string.IsNullOrEmpty(path)&&AssetDatabase.AssetPathExists(path))AssetDatabase.DeleteAsset(path);}
}
