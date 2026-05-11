using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using CUHP;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
    //[SerializeField] private HeatMapVisual heatMapVisual;
    [SerializeField] private CUHP.Grid<GridUnit> grid;
    [SerializeField] private Texture2D gridBackgroundTexture;
    
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cellSize = 1f;
    
    void Awake()
    {
        grid = new Grid<GridUnit>(gridWidth, gridHeight, cellSize, transform.position, (CUHP.Grid<GridUnit> g,int x,int y)=> new GridUnit(g,x,y));
        //heatMapVisual.SetGrid(grid);
        CreateGridBackgroundMesh();
    }
    
    private void CreateGridBackgroundMesh()
    {
        if (gridBackgroundTexture == null)
        {
            Debug.LogError("Grid Background Texture is not assigned!");
            return;
        }
        
        // Create a new GameObject for the grid background
        GameObject backgroundObj = new GameObject("GridBackground");
        backgroundObj.transform.SetParent(transform);
        backgroundObj.transform.localPosition = Vector3.zero;
        
        // Calculate grid dimensions
        float gridWorldWidth = gridWidth * cellSize;
        float gridWorldHeight = gridHeight * cellSize;
        
        // Create mesh
        Mesh mesh = new Mesh();
        mesh.name = "GridBackgroundMesh";
        
        // Create vertices for the quad (4 corners)
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(0, 0, 0),                           // Bottom-left
            new Vector3(gridWorldWidth, 0, 0),              // Bottom-right
            new Vector3(gridWorldWidth, gridWorldHeight, 0), // Top-right
            new Vector3(0, gridWorldHeight, 0)              // Top-left
        };
        
        // Create UV coordinates - tile texture for each grid cell
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(gridWidth, 0),
            new Vector2(gridWidth, gridHeight),
            new Vector2(0, gridHeight)
        };
        
        // Create triangles (2 triangles for a quad)
        int[] triangles = new int[6]
        {
            0, 2, 1, // First triangle
            0, 3, 2  // Second triangle
        };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Add MeshFilter
        MeshFilter meshFilter = backgroundObj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        // Add MeshRenderer
        MeshRenderer meshRenderer = backgroundObj.AddComponent<MeshRenderer>();
        
        // Create and assign material with custom shader
        Shader gridShader = Shader.Find("Custom/GridBackground");
        if (gridShader == null)
        {
            Debug.LogError("GridBackground shader not found! Make sure the shader is in the Shaders folder.");
            gridShader = Shader.Find("Standard");
        }
        Material material = new Material(gridShader);
        material.mainTexture = gridBackgroundTexture;
        meshRenderer.material = material;
        
        Debug.Log("Grid background mesh created successfully!");
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();
            // HeatMapGridObject gridObject= grid.GetGridObject(mousePosition);
            // if (gridObject!=null)
            // {
            //     gridObject.AddValue(2);
            //     grid.SetGridObject(mousePosition, gridObject);
            // }
        }
    }

}


