using UnityEngine;

// Partículas de estallido
public static class FX
{
    private static Material mat;

    public static void Burst(Vector3 pos, Color color, int count = 30)
    {
        GameObject go = new GameObject("FX");
        go.transform.position = pos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        // Detener antes de configurar
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.8f;
        main.startSpeed = 2.5f;
        main.startSize = 0.12f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        // Si el shader no está en la build, no se crea el material (evita excepciones que frenan a los policías)
        if (mat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Particles/Standard Unlit");
            if (s != null) mat = new Material(s);
        }
        if (mat != null) r.sharedMaterial = mat;

        ps.Play();
    }
}
