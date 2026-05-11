using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using System;

namespace CUHP
{
    public class Grid<TGridObject>
    {
        public EventHandler<OnGridObjectChangedEventArg> OnGridObjectChanged;
        public class OnGridObjectChangedEventArg : EventArgs {
            public int x, y;
        }

        private int width;
        private int height;
        private float cellSize;
        private Transform parent;
        private TGridObject[,] gridArray;
        private Vector3 originPosition;
        private TextMesh[,] debugTextArray;

        public Grid(int width, int height, float cellSize, Vector3 originPosition, Func<Grid<TGridObject>, int,int, TGridObject> createGridObject, bool showDebug = false)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            gridArray = new TGridObject[width, height];
            debugTextArray = new TextMesh[width, height];
            this.originPosition = originPosition;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    gridArray[i, j] = createGridObject(this, i, j);
                }
            }
            // showDebug is now controlled by the constructor parameter (default: false)
            if (showDebug)
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        debugTextArray[i, j] = UtilsClass.CreateWorldTextWithCharacterSize(i+","+j, null, GetWorldPosition(i, j) + new Vector3(cellSize, cellSize) * 0.5f, 0.2f, 20, Color.white, TextAnchor.MiddleCenter);
                        Debug.DrawLine(GetWorldPosition(i, j), GetWorldPosition(i, j + 1), Color.white, 100f);
                        Debug.DrawLine(GetWorldPosition(i, j), GetWorldPosition(i + 1, j), Color.white, 100f);
                    }
                }
                Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);
                Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
            }
        }
        public Vector3 GetWorldPosition(int x,int y)
        {
            return new Vector3(x,y)*cellSize+originPosition;
        }
        public Vector3 GetCenterWorldPosition(int x, int y)
        {
            return GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * 0.5f;
        }
        private void GetXY(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
            y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
        }
        public int GetWidth()
        {
            return width;
        }

        public int GetHeight()
        {
            return height;
        }

        public float GetCellSize()
        {
            return cellSize;
        }
        public Vector3 GetCenter()
        {
            return GetWorldPosition(width/2,height/2);
        }
        public void SetGridObject(int x,int y,TGridObject value)
        {
            if(x>=0&&y>=0&&x<width && y < height)
            {
                gridArray[x, y] = value;
                if (debugTextArray[x, y] != null) debugTextArray[x, y].text = value.ToString();
                if (OnGridObjectChanged != null) OnGridObjectChanged(this, new OnGridObjectChangedEventArg { x = x, y = y });
            }
        }
        public void TriggerObjectChanged(int x,int y)
        {
            OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArg {x = x, y = y});
        }
        public TGridObject GetGridObject(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                return gridArray[x, y];
            }
            else
            {
                return default(TGridObject);
            }
        }
        public void SetGridObject(Vector2 worldPosition, TGridObject value)
        {
            int x, y;
            GetXY(worldPosition,out x,out y);
            SetGridObject(x,y,value);
        }
        public TGridObject GetGridObject(Vector2 worldPosition)
        {
            int x, y;
            GetXY(worldPosition, out x, out y);
            return GetGridObject(x, y);
        }
    }
}
