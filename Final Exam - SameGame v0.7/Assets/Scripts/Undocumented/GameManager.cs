using UnityEngine;

public class GameManager : MonoBehaviour
{
    //---Member---
    public Vector2 cellSize = new Vector2(1.2f, 1.2f);
    public int numberOfCellsPerRow = 15;
    public int numberOfCellsPerColumn = 15;
    public int randomNumberSeed;

    Level currentLevel;
    bool isPlaying = false;

    [SerializeField]
    GameObject[] prefabs;
    [SerializeField]
    Camera playerCamera;
    [SerializeField]
    GUIStyle centerAlignmentStyle;

    // GUI
    string guiMessage;
    Vector2 guiSize;
    Vector2 messageBoxSize;
    Rect guiRect;
    Rect messageBoxRect;



    //---Unity Callbacks---
    void Start()
    {
        guiSize = new Vector2(200, 60);
        messageBoxSize = guiSize + new Vector2(40, 40);
    }

    // Update is called once per frame
    void Update()
    {
        MouseListener();
    }

    void OnGUI()
    {
        if (GUILayout.Button("New Game"))
        {
            NewLevel(numberOfCellsPerRow, numberOfCellsPerColumn, cellSize, prefabs);
        }

        if (currentLevel != null)
        {
            GUILayout.Label("Points: " + currentLevel.Points);
        }

        if (!isPlaying && currentLevel != null)
        {
            guiRect = new Rect((Screen.width - guiSize.x) * 0.5f, (Screen.height - guiSize.y) * 0.5f, guiSize.x, guiSize.y);
            messageBoxRect = new Rect((Screen.width - messageBoxSize.x) * 0.5f, (Screen.height - messageBoxSize.y) * 0.5f, messageBoxSize.x, messageBoxSize.y);

            GUI.Box(messageBoxRect, "");
            GUILayout.BeginArea(guiRect);
            GUILayout.Label(guiMessage, centerAlignmentStyle);
            GUILayout.Space(10);
            GUILayout.Label("Your Score: " + currentLevel.Points, centerAlignmentStyle);
            GUILayout.EndArea();
        }
    }


    //---Methods---
    void NewLevel(int numberOfCellsPerRow, int numberOfCellsPerColumn, Vector2 cellSize, GameObject[] prefabs)
    {

        // Setup Cam
        playerCamera.transform.position = new Vector3(0, 0, -10);
        playerCamera.transform.rotation = Quaternion.identity;
        playerCamera.transform.localScale = Vector3.one;
        playerCamera.orthographic = true;
        playerCamera.orthographicSize = 10;
        playerCamera.backgroundColor = new Color(3f / 255f, 18f / 255f, 41f / 255f);


        // Setup this gameObject
        transform.position = new Vector3(-(cellSize.x * numberOfCellsPerRow * 0.5f), -(cellSize.y * numberOfCellsPerColumn * 0.5f), 0f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (currentLevel != null)
        {
            currentLevel.DestroyLevel();
        }

        // Create new Level
        currentLevel = new Level(transform.position, cellSize, numberOfCellsPerRow, numberOfCellsPerColumn, randomNumberSeed, prefabs, transform);

        for(int i = 0; i < currentLevel.Grid.CellCount; i++)
        {
            Instantiate(currentLevel.Grid.GetElement(i).Visuals, currentLevel.Grid.GetCellCenter(i), Quaternion.identity);
        }

        isPlaying = true;
    }

    void MouseListener()
    {
        if (currentLevel == null || !isPlaying) return;

        if (Input.GetMouseButtonUp(0))
        {
            OnMouseClick();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            // do nothing
        }
        else
        {
            OnMouseHover();
        }
    }

    void OnMouseHover()
    {
        currentLevel.HoverCells(playerCamera.ScreenToWorldPoint(Input.mousePosition));
    }

    void OnMouseClick()
    {
        int numberOfSelectedCells = currentLevel.SelectCells(playerCamera.ScreenToWorldPoint(Input.mousePosition));

        switch (currentLevel.CheckLevelState())
        {
            case Level.LevelState.NoElementsLeft: Win(); break;
            case Level.LevelState.NoMoreMovesPossible: Loose(numberOfSelectedCells); break;
        }
    }

    void Win()
    {
        currentLevel.Points += 1000;
        guiMessage = "*** You Solved it! ***";
        Debug.Log("*** You Solved it! ***");
        Debug.Log("*** Your Score: " + currentLevel.Points);
        isPlaying = false;
    }
    void Loose(int numberOfRemainingCells)
    {

        // Loose
        currentLevel.Points -= currentLevel.CalculatePoints(numberOfRemainingCells);
        guiMessage = "*** No more moves possible! ***";
        Debug.Log("*** No more moves possible! ***");
        Debug.Log("*** Your Score: " + currentLevel.Points);
        isPlaying = false;
    }
}
