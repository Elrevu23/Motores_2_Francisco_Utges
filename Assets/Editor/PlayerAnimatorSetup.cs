using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Crear Animator del jugador
public static class PlayerAnimatorSetup
{
    private const string UalPath = "Assets/Animations/Universal Animation Library[Standard]/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx";
    public const string ControllerPath = "Assets/Animations/Jugador.controller";

    // Recrear el controller
    public static AnimatorController EnsureController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        ModelImporter importer = AssetImporter.GetAtPath(UalPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("No se encontró UAL1_Standard.fbx en Assets/Animations.");
            return null;
        }

        // Clips Humanoid y loop
        importer.animationType = ModelImporterAnimationType.Human;
        importer.SaveAndReimport();

        importer = (ModelImporter)AssetImporter.GetAtPath(UalPath);
        ModelImporterClipAnimation[] clipSettings = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation c in clipSettings)
        {
            if (c.name.Contains("Loop"))
            {
                c.loopTime = true;
                // Loop Pose del clip
                c.loopPose = true;
            }

            // Hornear raíz del clip
            c.lockRootRotation = true;
            // Orientación de los clips
            c.keepOriginalOrientation = true;
            c.rotationOffset = 180f;
            c.lockRootHeightY = true;
            c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true;
            c.keepOriginalPositionXZ = true;
        }
        importer.clipAnimations = clipSettings;
        importer.SaveAndReimport();

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Crouching", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        BlendTree loco = CreateBlend(ctrl, "Movimiento", Clip("Idle_Loop"), Clip("Walk_Loop"), Clip("Jog_Fwd_Loop"));
        BlendTree crouch = CreateBlend(ctrl, "Agachado", Clip("Crouch_Idle_Loop"), Clip("Crouch_Fwd_Loop"));

        AnimatorState stLoco = sm.AddState("Movimiento");
        stLoco.motion = loco;
        AnimatorState stCrouch = sm.AddState("Agachado");
        stCrouch.motion = crouch;
        AnimatorState stJump = sm.AddState("Salto");
        stJump.motion = Clip("Jump_Loop");
        sm.defaultState = stLoco;

        // Transiciones de movimiento
        AnimatorStateTransition toCrouch = stLoco.AddTransition(stCrouch);
        toCrouch.hasExitTime = false;
        toCrouch.duration = 0.15f;
        toCrouch.AddCondition(AnimatorConditionMode.If, 0, "Crouching");

        AnimatorStateTransition toStand = stCrouch.AddTransition(stLoco);
        toStand.hasExitTime = false;
        toStand.duration = 0.15f;
        toStand.AddCondition(AnimatorConditionMode.IfNot, 0, "Crouching");

        // Transición a salto
        AnimatorStateTransition toJump = sm.AddAnyStateTransition(stJump);
        toJump.hasExitTime = false;
        toJump.duration = 0.1f;
        toJump.canTransitionToSelf = false;
        toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

        // Salida del salto
        AnimatorStateTransition toLanding = stJump.AddTransition(stLoco);
        toLanding.hasExitTime = false;
        toLanding.duration = 0.1f;
        toLanding.AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        AnimatorStateTransition toLandCrouch = stJump.AddTransition(stCrouch);
        toLandCrouch.hasExitTime = false;
        toLandCrouch.duration = 0.1f;
        toLandCrouch.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        toLandCrouch.AddCondition(AnimatorConditionMode.If, 0, "Crouching");

        // Caer de un borde
        AnimatorStateTransition fallLoco = stLoco.AddTransition(stJump);
        fallLoco.hasExitTime = false;
        fallLoco.duration = 0.2f;
        fallLoco.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

        AnimatorStateTransition fallCrouch = stCrouch.AddTransition(stJump);
        fallCrouch.hasExitTime = false;
        fallCrouch.duration = 0.2f;
        fallCrouch.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    // Buscar clip de UAL1
    static AnimationClip Clip(string name)
    {
        AnimationClip[] allClips = AssetDatabase.LoadAllAssetsAtPath(UalPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        AnimationClip clip = allClips.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name));
        if (clip == null)
            Debug.LogWarning($"Clip no encontrado: {name}. Clips disponibles: {string.Join(", ", allClips.Select(c => c.name))}");
        return clip;
    }

    // Animator de policías
    public const string PoliceControllerPath = "Assets/Animations/Policia.controller";

    public static AnimatorController EnsurePoliceController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PoliceControllerPath) != null)
            AssetDatabase.DeleteAsset(PoliceControllerPath);

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(PoliceControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

        BlendTree loco = CreateBlend(ctrl, "Patrulla", Clip("Idle_Loop"), Clip("Walk_Loop"), Clip("Jog_Fwd_Loop"));
        AnimatorState st = ctrl.layers[0].stateMachine.AddState("Patrulla");
        st.motion = loco;
        ctrl.layers[0].stateMachine.defaultState = st;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    // Árbol de movimiento (Speed)
    static BlendTree CreateBlend(AnimatorController ctrl, string name, params AnimationClip[] clips)
    {
        BlendTree tree = new BlendTree
        {
            name = name,
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed"
        };
        AssetDatabase.AddObjectToAsset(tree, ctrl);

        for (int i = 0; i < clips.Length; i++)
            tree.AddChild(clips[i], i);

        return tree;
    }
}
