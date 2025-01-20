using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LiquidContainer : MonoBehaviour
{
    private static readonly int WobbleXShaderName = Shader.PropertyToID("_WobbleX");
    private static readonly int WobbleZShaderName = Shader.PropertyToID("_WobbleZ");
    private static readonly int FillAmountShaderName = Shader.PropertyToID("_FillAmount");
    private static readonly int LiquidColorShaderName = Shader.PropertyToID("_LiquidColor");

    public Action<float> Spilled;

    [Header("Container")]
    [SerializeField]
    [Tooltip("How many milliliters this container can hold")]
    private int containerVolume = 200;
    [SerializeField]
    [Tooltip("TooltipText")] // TODO Arthur
    private bool isMeshOriginOnCenter;
    [SerializeField]
    [Tooltip("Reference to the liquid meshRenderer")]
    private MeshRenderer meshRenderer;
    [SerializeField]
    [Tooltip("[Optional] Reference to a particle system to indicate the liquid is rotating")]
    private ParticleSystem stirringParticleSystem;




    // Woble

    Renderer rend;
    Vector3 lastPos;
    Vector3 velocity;
    Vector3 lastRot;
    Vector3 angularVelocity;
    public float MaxWobble = 0.03f;
    public float WobbleSpeed = 1f;
    public float Recovery = 1f;
    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float pulse;
    float time = 0.5f;
    [SerializeField]
    [Tooltip("[Optional] If a non ingredient mesh should be updated as submerged.\nThe submergedShader must be applied manually to the desired materials")]
    private MeshRenderer defaultSubmergedMesh;
    private float elapsedTime;
    


    // Container Variables
    private const int pourPointsAmount = 10;
    private List<Transform> pourPoints;
    private List<Transform> basePoints;

    private float availableLocalHeight;
    private float extraLocalHeight;
    private float containerRadius;

    private float containerVolumePerHeight;
    private float currentLiquidHeight;
    private float currentLiquidVolume;

    private Transform lowestPourPoint;
    private Transform highestPourPoint;
    private Transform lowestBasePoint;


    // References
    private Transform _transform;
    private Renderer _renderer;

    public Transform GetCurrentPourPoint => lowestPourPoint;

    private Color LiquidColor => meshRenderer.material.GetColor(LiquidColorShaderName);
    private Shader submergedShader;
    private readonly Dictionary<MeshRenderer, List<Shader>> submergedMeshRendererOriginalShaders
        = new Dictionary<MeshRenderer, List<Shader>>();

    public void AddLiquid(float volume, Material newMaterial)
    {
        currentLiquidVolume += volume;
        currentLiquidHeight += volume / containerVolumePerHeight;

        if (newMaterial != meshRenderer.material)
        {
            meshRenderer.material = newMaterial;
            UpdateShaderFillAmount();
            UpdateSubmergedMaterialColor(LiquidColor);
        }
    }

    public void Empty()
    {
        currentLiquidHeight = 0;
        currentLiquidVolume = 0;
    }

    public void StartStirring()
    {
        if (currentLiquidVolume > 0 && !stirringParticleSystem.isPlaying)
        {
            stirringParticleSystem.Play();
        }
    }

    public void StopStirring()
    {
        stirringParticleSystem.Stop();
    }
    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _transform = transform;

        submergedShader = Shader.Find("Custom/SubmergedLit");
        if (defaultSubmergedMesh != null)
        {
            SubmergeMeshRenderer(defaultSubmergedMesh, false);
        }

        InitializeConstantVariables();
        UpdateShaderFillAmount();
        UpdateSubmergedMaterialFill();
    }


    private void UpdateSubmergedMaterialFill()
    {
        foreach (var submergedRenderer in submergedMeshRendererOriginalShaders.Keys)
        {
            var submergedPosition = submergedRenderer.transform.position;

            foreach (var material in submergedRenderer.materials)
            {
                SetSubmergedMaterialFillAmount(submergedPosition, material);
            }
        }
    }

    private void UpdateSubmergedMaterialColor(Color color)
    {
        foreach (var submergedRenderer in submergedMeshRendererOriginalShaders.Keys)
        {
            foreach (var material in submergedRenderer.materials)
            {
                SetSubmergedMaterialColor(material, color);
            }
        }
    }

    private void SetSubmergedMaterialColor(Material material, Color color)
    {
        if (material.shader == submergedShader)
        {
            material.SetColor(LiquidColorShaderName, color);
        }
    }

    private void SetSubmergedMaterialFillAmount(Vector3 submergedPosition, Material material)
    {
        if (material.shader == submergedShader)
        {
            var fill = transform.position.y - submergedPosition.y + currentLiquidHeight;
            material.SetFloat(FillAmountShaderName, fill);
        }
    }
    void Update()
    {
        lowestPourPoint = GetLowestPoint(pourPoints);
        highestPourPoint = GetHighestPoint(pourPoints);
        lowestBasePoint = GetLowestPoint(basePoints);

        Wobble();

        TrySpill();
        UpdateShaderFillAmount();
        UpdateSubmergedMaterialFill();
    }
    #region Container Logic

    private Transform GetLowestPoint(List<Transform> points)
    {
        Transform lowestPoint = null;
        float lowestHeight = Mathf.Infinity;

        foreach (var point in points)
        {
            if (point.position.y < lowestHeight)
            {
                lowestHeight = point.position.y;
                lowestPoint = point;
            }
        }

        return lowestPoint;
    }

    private Transform GetHighestPoint(List<Transform> points)
    {
        Transform highestPoint = null;
        float highestHeight = Mathf.NegativeInfinity;

        foreach (var point in points)
        {
            if (point.position.y > highestHeight)
            {
                highestHeight = point.position.y;
                highestPoint = point;
            }
        }

        return highestPoint;
    }

    private float GetLiquidLocalHeight()
    {
        // offset from range [0, height] to [-height/2, height/2] if the origin is in the center
        var offsetHeight = availableLocalHeight * (isMeshOriginOnCenter ? 0.5f : 0);

        return currentLiquidHeight
             - offsetHeight
             - extraLocalHeight; // remove the extra height that can't hold liquid when rotated
    }

    private void Spill(float volumeHeightSpilled)
    {
        if (currentLiquidHeight <= 0)
        {
            return;
        }

        var previousHeight = currentLiquidHeight;
        currentLiquidHeight = Mathf.Max(0, currentLiquidHeight - volumeHeightSpilled);
        var volumeSpilled = (1 - (currentLiquidHeight / previousHeight)) * currentLiquidVolume;
        currentLiquidVolume -= volumeSpilled;

        Spilled?.Invoke(volumeSpilled);
    }

    private void TrySpill()
    {
        var pourYPosition = lowestPourPoint.position.y;
        availableLocalHeight = (pourYPosition - lowestBasePoint.position.y);
        extraLocalHeight = (highestPourPoint.position.y - pourYPosition) * 0.5f;
        containerVolumePerHeight = containerVolume / (Mathf.Abs(availableLocalHeight) + extraLocalHeight);

        if (currentLiquidHeight > availableLocalHeight)
        {
            Spill(currentLiquidHeight - availableLocalHeight);
        }
    }

    private void UpdateShaderFillAmount()
    {
        float shaderFill = currentLiquidHeight <= 0 ? -500 : GetLiquidLocalHeight();
        _renderer.material.SetFloat(FillAmountShaderName, shaderFill);
    }

    #endregion

    private void SubmergeMeshRenderer(MeshRenderer submergedRenderer, bool changeShader = true)
    {
        submergedMeshRendererOriginalShaders.Add(submergedRenderer, submergedRenderer.materials.Select(material => material.shader).ToList());

        var submergedPosition = submergedRenderer.transform.position;
        var color = LiquidColor;

        foreach (var material in submergedRenderer.materials)
        {
            if (changeShader)
            {
                material.shader = submergedShader;
            }

            SetSubmergedMaterialColor(material, color);
            SetSubmergedMaterialFillAmount(submergedPosition, material);
        }
    }
    private void Wobble()
    {
        time += Time.deltaTime;
        // decrease wobble over time
        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * (Recovery));
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * (Recovery));

        // make a sine wave of the decreasing wobble
        pulse = 2 * Mathf.PI * WobbleSpeed;
        wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

        // send it to the shader
        rend.material.SetFloat("_WobbleX", wobbleAmountX);
        rend.material.SetFloat("_WobbleZ", wobbleAmountZ);

        // velocity
        velocity = (lastPos - transform.position) / Time.deltaTime;
        angularVelocity = transform.rotation.eulerAngles - lastRot;


        // add clamped velocity to wobble
        wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
        wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);

        // keep last position
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }

    private void CreatePoints(List<Transform> points, float verticalOffset, string groupName)
    {
        Transform pointsParent = new GameObject(groupName).transform;
        pointsParent.parent = _transform;
        pointsParent.rotation = Quaternion.identity;
        pointsParent.localScale = Vector3.one;
        pointsParent.localPosition = new Vector3(0, verticalOffset, 0);

        for (int index = 0; index < pourPointsAmount; index++)
        {
            Transform newPoint = new GameObject($"Point{index}").transform;
            newPoint.parent = pointsParent;
            newPoint.rotation = Quaternion.identity;
            newPoint.localScale = Vector3.one;

            var x = containerRadius * Mathf.Sin((2 * Mathf.PI * index) / pourPointsAmount);
            var z = containerRadius * Mathf.Cos((2 * Mathf.PI * index) / pourPointsAmount);
            newPoint.localPosition = new Vector3(x, 0, z);

            points.Add(newPoint);
        }
    }

    private void InitializeConstantVariables()
    {
        // Container Variables
        var bounds = _renderer.bounds;
        var localScale = _transform.localScale;

        Vector3 containerSize = bounds.size;
        availableLocalHeight = containerSize.y / localScale.y;
        containerVolumePerHeight = containerVolume / (availableLocalHeight + extraLocalHeight);

        containerRadius = containerSize.x * 0.5f / localScale.x;

        var maxPoint = _transform.InverseTransformPoint(bounds.max);
        var minPoint = _transform.InverseTransformPoint(bounds.min);

        pourPoints = new List<Transform>();
        CreatePoints(pourPoints, maxPoint.y, "PourPoints");

        basePoints = new List<Transform>();
        CreatePoints(basePoints, minPoint.y, "BasePoints");

        // Wobble Variables
        pulse = 2 * Mathf.PI * WobbleSpeed;
    }


}
