using Celeste.Mod.Celeste3DEngine;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteChess;

public class BackgroundVisualiser
{
    public static void Load3DEnine(EngineEntity e, Scene s)
    {
        Scene3D scene3D = new Scene3D();
        
        Camera3D cam = new Camera3D();
        cam.FOV = 30f;
        cam.transform.SetPosition(new Vector3(-3.95f, 2.8f, 0f));
        Quaternion qBlender = new Quaternion(0.369f, -0.369f, -0.603f, 0.603f);
        Quaternion qM = new Quaternion(-0.7071068f, 0f, 0f, 0.7071068f);
        Quaternion rot = Quaternion.Normalize(qM * qBlender);

        cam.transform.SetRotation(rot);
        scene3D.AddGameObject(cam);
        scene3D.SetMainCamera(cam);
        //scene3D.EnableSkybox(false);

        scene3D.GetRenderer().SetHDMode(true);
        EngineEntity.LoadScene(scene3D);
        
        WindSettings wind = new WindSettings();
        wind.frequency = 0.45f;
        wind.strength = 0.10f;
        scene3D.SetWindSettings(wind);

        LightingSettings lighting = new LightingSettings();
        lighting.DirectionalLightColor = new Vector3(0.5f, 0.25f, 0.25f);
        scene3D.SetLightingSettings(lighting);

        PointLight pl = new PointLight(new Vector3(1.85f, 2f, 0f), Color.DarkOrange);
        scene3D.AddGameObject(pl);
        
        
        string path = EnginePaths.CustomModelsPath + "/Placements.json";
        var placements = JSONPlacements.GetPlacements(path);

        foreach (var (modelName, trans) in placements)
        {
            GameObject obj = new GameObject();
            obj.SetTransform(trans);
            MeshRenderer mr = new MeshRenderer(modelName);
            obj.AddComponent(mr);
            scene3D.AddGameObject(obj);
            
            if (modelName.Contains("Frames"))
            {
                mr.castsShadows = false;
            }
        }
        
        path = EnginePaths.CustomModelsPath + "/Scene.json";
        var scenePlacements = JSONPlacements.GetPlacements(path);
        
        foreach (var (modelName, trans) in scenePlacements)
        {
            GameObject obj = new GameObject();
            obj.SetTransform(trans);
            MeshRenderer mr = new MeshRenderer(modelName);
            obj.AddComponent(mr);
            scene3D.AddGameObject(obj);
        }
        
        path = EnginePaths.CustomModelsPath + "/Plants.json";
        var plantsPlacemnts = JSONPlacements.GetPlacements(path);
        
        foreach (var (modelName, trans) in plantsPlacemnts)
        {
            GameObject obj = new GameObject();
            obj.SetTransform(trans);
            MeshRenderer mr = new MeshRenderer(modelName);
            obj.AddComponent(mr);
            scene3D.AddGameObject(obj);

            if (modelName.Contains("Leaves"))
            {
                mr.useWind = true;
            }
        }
        
        path = EnginePaths.CustomModelsPath + "/Pieces.json";
        var piecesPlacements = JSONPlacements.GetPlacements(path);
        
        foreach (var (modelName, trans) in piecesPlacements)
        {
            GameObject obj = new GameObject();
            obj.SetTransform(trans);
            MeshRenderer mr = new MeshRenderer(modelName);
            obj.AddComponent(mr);
            scene3D.AddGameObject(obj);
        }
    }
}