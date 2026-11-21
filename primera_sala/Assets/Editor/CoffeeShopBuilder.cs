using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CoffeeShopBuilder
{
    private static Material _wood, _cream, _green, _metal, _ceramic, _bread, _glow, _glass;
    private static Transform _architecture, _furniture, _interactables, _lighting;
    private static UnityEngine.XR.Interaction.Toolkit.XRInteractionManager _manager;

    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        if (roots.Any(g => g.name == "--- ARCHITECTURE ---")) throw new System.InvalidOperationException("Café already assembled; do not duplicate it.");
        var rig = roots.SelectMany(g => g.GetComponentsInChildren<Unity.XR.CoreUtils.XROrigin>(true)).First().gameObject;
        var machine = roots.First(g => g.name == "cafetera");
        var provider = rig.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true);
        if (provider == null) throw new System.InvalidOperationException("Existing teleportation provider missing.");
        var oldArea = roots.SelectMany(g => g.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>(true)).First();
        int teleportMask = oldArea.interactionLayers.value;
        var existingCore = roots.FirstOrDefault(g => g.name == "--- VR CORE ---");
        var core = existingCore != null ? existingCore.transform : Group("--- VR CORE ---", null);
        foreach (var root in roots.Where(g => g == rig || g.name == "EventSystem" || g.name == "Hands Permissions Manager"))
        { Undo.SetTransformParent(root.transform, core, "Organize VR core"); }
        _manager = roots.SelectMany(g => g.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>(true)).FirstOrDefault();
        if (_manager == null) _manager = Group("XR Interaction Manager", core).gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        else Undo.SetTransformParent(_manager.transform, core, "Preserve manager");
        foreach (var t in rig.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Affordance Callouts") || t.name.StartsWith("Joystick Affordances")).ToArray())
        {
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }
        foreach (var root in roots.Where(g => new[] { "Interactables", "UI", "Environment", "Teleport Area Setup", "Lighting" }.Contains(g.name))) Undo.DestroyObjectImmediate(root);
        _architecture = Group("--- ARCHITECTURE ---", null);
        _furniture = Group("--- STATIC FURNITURE ---", null);
        _interactables = Group("--- VR INTERACTABLES ---", null);
        _lighting = Group("--- LIGHTING ---", null);
        System.IO.Directory.CreateDirectory("Assets/CoffeeShop/Materials");
        AssetDatabase.Refresh();
        _wood = Mat("Walnut", new Color(.27f,.13f,.065f), 0,.32f);
        _cream = Mat("Warm Plaster", new Color(.84f,.77f,.63f),0,.18f);
        _green = Mat("Sage Upholstery", new Color(.19f,.29f,.21f),0,.2f);
        _metal = Mat("Brushed Brass", new Color(.53f,.35f,.12f),.8f,.65f);
        _ceramic = Mat("Ivory Ceramic", new Color(.94f,.89f,.76f),0,.65f);
        _bread = Mat("Golden Pastry",new Color(.66f,.32f,.09f),0,.2f);
        _glow = Mat("Warm Lamp",new Color(1,.8f,.45f),0,.3f);
        _glow.EnableKeyword("_EMISSION"); _glow.SetColor("_EmissionColor",new Color(1,.65f,.25f)*2); _glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
        _glass = Mat("Storefront Glass",new Color(.65f,.83f,.86f,.12f),0,.9f);
        _glass.SetFloat("_Surface",1); _glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); _glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha); _glass.SetFloat("_ZWrite",0); _glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); _glass.renderQueue=3000;
        var floor = Piece("Floor - Teleportation Area",_architecture,new Vector3(0,-.1f,0),new Vector3(12,.2f,10),_wood);
        var area = floor.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
        area.interactionManager=_manager; area.teleportationProvider=provider; area.interactionLayers=teleportMask; area.colliders.Clear(); area.colliders.Add(floor.GetComponent<Collider>());
        area.matchOrientation=UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.WorldSpaceUp;
        Piece("Back Wall",_architecture,new Vector3(0,1.6f,5),new Vector3(12.2f,3.2f,.2f),_cream);
        Piece("Left Wall",_architecture,new Vector3(-6,1.6f,0),new Vector3(.2f,3.2f,10),_cream);
        Piece("Right Wall",_architecture,new Vector3(6,1.6f,0),new Vector3(.2f,3.2f,10),_cream);
        Piece("Ceiling",_architecture,new Vector3(0,3.3f,0),new Vector3(12.2f,.2f,10.2f),_cream);
        Piece("Storefront Header",_architecture,new Vector3(0,2.95f,-5),new Vector3(12,.5f,.2f),_wood);
        for(int i=0;i<5;i++)
        {
            float x=-4.8f+i*2.4f;
            if(i==2) continue;
            Piece("Window Sill "+i,_architecture,new Vector3(x,.2f,-5),new Vector3(2.4f,.4f,.2f),_wood);
            var glass=Piece("Storefront Window "+i,_architecture,new Vector3(x,1.55f,-5),new Vector3(2.28f,2.3f,.025f),_glass);
            glass.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        for(int i=0;i<=5;i++) Piece("Window Mullion "+i,_architecture,new Vector3(-6+i*2.4f,1.6f,-5),new Vector3(.12f,3.2f,.18f),_wood);
        // Invisible boundary retains safe indoor teleport and locomotion space; entrance remains visually open.
        var threshold=Group("Entrance Safety Boundary",_architecture).gameObject;
        threshold.transform.localPosition=new Vector3(0,1.3f,-5); var boundary=threshold.AddComponent<BoxCollider>(); boundary.size=new Vector3(2.28f,2.6f,.08f);
        Piece("Counter Body",_furniture,new Vector3(4.35f,.51f,.8f),new Vector3(1.05f,1.02f,5.4f),_green);
        Piece("Countertop",_furniture,new Vector3(4.35f,1.06f,.8f),new Vector3(1.25f,.08f,5.6f),_wood);
        Piece("Back Bar",_furniture,new Vector3(5.65f,.49f,.8f),new Vector3(.4f,.98f,5.4f),_wood);
        for(int j=0;j<2;j++) Piece("Display Shelf "+j,_furniture,new Vector3(5.65f,1.65f+j*.55f,.8f),new Vector3(.42f,.06f,4.7f),_wood);
        Undo.RecordObject(machine.transform,"Place espresso machine");
        Undo.SetTransformParent(machine.transform,_furniture,"Organize machine");
        if(PrefabUtility.IsPartOfPrefabInstance(machine)) PrefabUtility.UnpackPrefabInstance(machine,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        foreach(var rb in machine.GetComponentsInChildren<Rigidbody>(true)) Undo.DestroyObjectImmediate(rb);
        foreach(var col in machine.GetComponentsInChildren<Collider>(true)) Undo.DestroyObjectImmediate(col);
        machine.transform.SetPositionAndRotation(new Vector3(4.3f,1.1f,1.5f),Quaternion.Euler(0,-90,0));
        var cupParts=machine.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Espresso_Cup") || t.name=="Espresso_Crema_Surface").ToArray();
        var cup=Group("Espresso Cup 01",_interactables);
        foreach(var part in cupParts) Undo.SetTransformParent(part,cup,"Separate imported cup");
        var cb=BoundsOf(cup.gameObject); cup.position=cb.center;
        foreach(var part in cupParts) part.position-=cb.center;
        cup.position=new Vector3(3.99f,1.1f+cb.extents.y+.008f,.45f);
        foreach(var part in cupParts) { var mesh=part.GetComponent<MeshFilter>(); if(mesh!=null){var mc=part.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh=mesh.sharedMesh;mc.convex=true;} }
        var handle=cupParts.First(t=>t.name.Contains("Handle")); Grab(cup.gameObject,.2f,handle.GetComponent<Renderer>().bounds.center);
        foreach(var mf in machine.GetComponentsInChildren<MeshFilter>()) { var mc=mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh=mf.sharedMesh; }
        var centers=new[]{new Vector3(-3.5f,0,-2.2f),new Vector3(-3.5f,0,1.5f),new Vector3(.1f,0,2.15f)};
        for(int i=0;i<centers.Length;i++)
        {
            var table=Group("Table "+(i+1),_furniture);table.position=centers[i];
            Piece("Tabletop",table,new Vector3(0,.735f,0),new Vector3(1.15f,.07f,1.15f),_wood);
            Piece("Pedestal",table,new Vector3(0,.35f,0),new Vector3(.14f,.7f,.14f),_metal);
            Piece("Base",table,new Vector3(0,.035f,0),new Vector3(.65f,.07f,.65f),_metal);
            int seats=i==2?4:2;
            for(int j=0;j<seats;j++)
            {
                float a=j*360f/seats; var chair=Group("Chair "+(i+1)+"-"+(j+1),_interactables);
                chair.position=centers[i]+Quaternion.Euler(0,a,0)*new Vector3(0,.006f,1.05f); chair.rotation=Quaternion.Euler(0,a,0);
                Piece("Seat",chair,new Vector3(0,.445f,0),new Vector3(.48f,.09f,.48f),_green);
                Piece("Backrest",chair,new Vector3(0,.73f,.2f),new Vector3(.48f,.48f,.07f),_wood);
                for(int k=0;k<4;k++) Piece("Leg "+k,chair,new Vector3(k%2==0?-.19f:.19f,.2f,k<2?-.19f:.19f),new Vector3(.045f,.4f,.045f),_wood);
                Grab(chair.gameObject,5,chair.TransformPoint(new Vector3(0,.9f,.2f)));
            }
            var saucer=Piece("Saucer "+(i+1),_interactables,centers[i]+new Vector3(.15f,.789f,0),new Vector3(.16f,.015f,.16f),_ceramic,PrimitiveType.Cylinder); Grab(saucer,.2f,saucer.transform.position);
            var copy=Object.Instantiate(cup.gameObject,_interactables);copy.name="Espresso Cup "+(i+2);Undo.RegisterCreatedObjectUndo(copy,"Cup");
            var b=BoundsOf(copy); copy.transform.position+=centers[i]+new Vector3(.15f,.808f,0)-new Vector3(b.center.x,b.min.y,b.center.z);
            Pendant(centers[i]+new Vector3(0,2.45f,0));
        }
        for(int i=0;i<2;i++)
        {
            var tray=Group("Serving Tray "+(i+1),_interactables); tray.position=new Vector3(4.2f,1.118f,-.8f+i*.6f);
            Piece("Tray Base",tray,Vector3.zero,new Vector3(.38f,.018f,.27f),_wood);
            for(int j=0;j<2;j++) {Piece("Long Rim",tray,new Vector3(0,.025f,j==0?-.13f:.13f),new Vector3(.38f,.04f,.015f),_wood);Piece("Short Rim",tray,new Vector3(j==0?-.18f:.18f,.025f,0),new Vector3(.015f,.04f,.25f),_wood);}
            Grab(tray.gameObject,.5f,tray.position+new Vector3(.18f,.025f,0));
            var pastry=Piece("Pastry "+(i+1),_interactables,new Vector3(4.2f,1.164f,-.8f+i*.6f),new Vector3(.12f,.065f,.08f),_bread,PrimitiveType.Sphere);Grab(pastry,.2f,pastry.transform.position);
        }
        for(int i=0;i<3;i++)
        {
            var take=Group("Takeaway Cup "+(i+1),_interactables);take.position=new Vector3(4.45f,1.106f,-1.65f+i*.2f);
            Piece("Paper Cup",take,new Vector3(0,.065f,0),new Vector3(.085f,.065f,.085f),_ceramic,PrimitiveType.Cylinder);
            Piece("Lid",take,new Vector3(0,.134f,0),new Vector3(.093f,.008f,.093f),_wood,PrimitiveType.Cylinder);
            Grab(take.gameObject,.25f,take.position+new Vector3(0,.06f,0));
        }
        var pitcher=Group("Milk Pitcher",_interactables);pitcher.position=new Vector3(4.22f,1.106f,2.12f);
        Piece("Pitcher Body",pitcher,new Vector3(0,.07f,0),new Vector3(.105f,.07f,.105f),_metal,PrimitiveType.Cylinder);
        Piece("Handle Top",pitcher,new Vector3(.077f,.112f,0),new Vector3(.05f,.012f,.015f),_metal);
        Piece("Handle Grip",pitcher,new Vector3(.098f,.075f,0),new Vector3(.012f,.085f,.015f),_metal);
        Piece("Handle Bottom",pitcher,new Vector3(.077f,.038f,0),new Vector3(.05f,.012f,.015f),_metal);
        Grab(pitcher.gameObject,.4f,pitcher.TransformPoint(new Vector3(.098f,.075f,0)));
        Pendant(new Vector3(4.3f,2.5f,-.8f));Pendant(new Vector3(4.3f,2.5f,1.7f));
        var sun=Group("Late Afternoon Sun",_lighting).gameObject.AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.8f,.57f);sun.intensity=1.15f;sun.lightmapBakeType=LightmapBakeType.Mixed;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(24,-20,0);RenderSettings.sun=sun;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.38f,.33f,.26f);RenderSettings.ambientEquatorColor=new Color(.24f,.21f,.16f);RenderSettings.ambientGroundColor=new Color(.12f,.105f,.08f);DynamicGI.UpdateEnvironment();
        var probe=Group("Cafe Reflection Probe",_lighting).gameObject.AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(0,1.6f,0);probe.size=new Vector3(12,3.2f,10);probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.resolution=128;probe.boxProjection=true;probe.blendDistance=.3f;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Cafe Warm Atmosphere";
        var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.16f);bloom.threshold.Override(1.1f);bloom.scatter.Override(.55f);
        var grading=profile.Add<ColorAdjustments>(true);grading.colorFilter.Override(new Color(1,.96f,.89f));grading.saturation.Override(-4);grading.contrast.Override(4);
        var balance=profile.Add<WhiteBalance>(true);balance.temperature.Override(8);
        AssetDatabase.CreateAsset(profile,"Assets/CoffeeShop/CafeVolume.asset");
        var volume=Group("Global Volume",_lighting).gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
        foreach(var cam in rig.GetComponentsInChildren<Camera>(true)) {Undo.RecordObject(cam,"Camera HDR");cam.allowHDR=true;var data=cam.GetUniversalAdditionalCameraData();Undo.RecordObject(data,"Cafe post processing");data.renderPostProcessing=true;}
        var urp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        Undo.RecordObject(urp,"Enable cafe lighting"); var so=new SerializedObject(urp);
        so.FindProperty("m_AdditionalLightsRenderingMode").intValue=1;so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=4;so.FindProperty("m_SupportsHDR").boolValue=true;so.FindProperty("m_ReflectionProbeBoxProjection").boolValue=true;so.FindProperty("m_ShadowDistance").floatValue=18;so.ApplyModifiedProperties();
        foreach(var root in new[]{_architecture,_furniture}) foreach(var r in root.GetComponentsInChildren<MeshRenderer>()) GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.ReflectionProbeStatic);
        Undo.RecordObject(rig.transform,"Cafe spawn");rig.transform.position=new Vector3(.2f,0,-3.6f);rig.transform.rotation=Quaternion.identity;
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
    }

    private static Transform Group(string name,Transform parent)
    { var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Create cafe object");g.transform.SetParent(parent,false);return g.transform; }
    private static Material Mat(string name,Color color,float metal,float smooth)
    { var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,"Assets/CoffeeShop/Materials/"+name+".mat");return m; }
    public static void FinalizeExisting()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots.Where(g => g.name != "--- VR CORE ---"))
        {
            foreach (var capsule in root.GetComponentsInChildren<CapsuleCollider>(true))
            {
                var go = capsule.gameObject;
                var components = go.GetComponents<Component>();
                Undo.RecordObject(go, "Correct cylinder collider");
                var mesh = go.GetComponent<MeshFilter>();
                if (mesh == null) continue;
                Undo.DestroyObjectImmediate(capsule);
                var collider = Undo.AddComponent<MeshCollider>(go);
                collider.sharedMesh = mesh.sharedMesh;
                collider.convex = go.GetComponentInParent<Rigidbody>() != null;
            }
            foreach (var grab in root.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(true))
            {
                Undo.RecordObject(grab, "Refresh grab colliders");
                grab.colliders.Clear(); grab.colliders.AddRange(grab.GetComponentsInChildren<Collider>());
            }
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            { Undo.RecordObject(renderer,"Clear stale lightmap assignment"); renderer.lightmapIndex = -1; }
        }
        // Detach, but do not delete, the old template bake and lighting settings assets.
        Lightmapping.lightingDataAsset = null;
        LightmapSettings.lightmaps = new LightmapData[0];
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>("Assets/CoffeeShop/CafeLighting.lighting");
        if (settings == null)
        {
            settings = new LightingSettings { name = "Cafe Lighting", bakedGI = true, realtimeGI = false, lightmapResolution = 20, lightmapMaxSize = 1024, indirectResolution = 2, mixedBakeMode = MixedLightingMode.IndirectOnly };
            AssetDatabase.CreateAsset(settings, "Assets/CoffeeShop/CafeLighting.lighting");
        }
        Lightmapping.lightingSettings = settings;
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static GameObject Piece(string name,Transform parent,Vector3 position,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube)
    {
        var g=GameObject.CreatePrimitive(type);Undo.RegisterCreatedObjectUndo(g,"Create cafe geometry");g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;
        if (type == PrimitiveType.Cylinder)
        {
            Undo.DestroyObjectImmediate(g.GetComponent<Collider>());
            var mc=g.AddComponent<MeshCollider>();mc.sharedMesh=g.GetComponent<MeshFilter>().sharedMesh;mc.convex=true;
        }
        return g;
    }
    private static Bounds BoundsOf(GameObject g)
    {var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    private static void Grab(GameObject g,float mass,Vector3 grip)
    {
        foreach(var t in g.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        var rb=g.AddComponent<Rigidbody>();rb.mass=mass;rb.angularDamping=2.5f;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        var grab=g.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();grab.interactionManager=_manager;grab.movementType=UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;grab.colliders.Clear();grab.colliders.AddRange(g.GetComponentsInChildren<Collider>());
        grab.throwOnDetach=false;grab.retainTransformParent=true;
        var attach=Group("Attach Transform - Grip",g.transform);attach.position=grip;grab.attachTransform=attach;
    }
    private static void Pendant(Vector3 p)
    {
        var lamp=Group("Pendant Lamp",_lighting);lamp.position=p;
        Piece("Brass Shade",lamp,Vector3.zero,new Vector3(.4f,.09f,.4f),_metal,PrimitiveType.Cylinder);
        Piece("Warm Diffuser",lamp,new Vector3(0,-.095f,0),new Vector3(.32f,.008f,.32f),_glow,PrimitiveType.Cylinder);
        float h=3.2f-p.y;Piece("Suspension",lamp,new Vector3(0,h*.5f,0),new Vector3(.015f,h*.5f,.015f),_metal,PrimitiveType.Cylinder);
        var l=Group("Warm Table Spotlight",lamp).gameObject.AddComponent<Light>();l.transform.localPosition=new Vector3(0,-.13f,0);l.transform.localRotation=Quaternion.Euler(90,0,0);l.type=LightType.Spot;l.color=new Color(1,.73f,.43f);l.intensity=2.5f;l.range=5;l.spotAngle=100;l.innerSpotAngle=70;l.lightmapBakeType=LightmapBakeType.Mixed;l.shadows=LightShadows.None;
    }
}
