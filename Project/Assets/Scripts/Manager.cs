using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using DG.Tweening;
using TMPro;

public class Manager : MonoBehaviour
{
	public int width = 4;
	public int height = 4;
	private int score = 0;
	[SerializeField] public TextMeshProUGUI scoreText;

	[SerializeField] private Node nodePrefab;
	[SerializeField] private Cell cellPrefab;
	[SerializeField] private SpriteRenderer board;


	[SerializeField] private SpriteRenderer boardPrefab;
	[SerializeField] private List<cellType> types;
	[SerializeField] private float travelTime = 0.2f;
	[SerializeField] private int winCondition = 2048;
	[SerializeField] private GameObject winScreen, loseScreen, menuScreen;
	[SerializeField] private Slider sizeSlider;
	[SerializeField] public TextMeshProUGUI sliderText;

	private List<Node> nodes;
	private List<Cell> cells;
	private gameState state;
	private int round;

	private cellType getCellTypeByValue(int value) => types.First(t => t.value == value);

	private Vector2 startTouchPos;
	private Vector2 currentTouchPos;
	private Vector2 endTouchPosition;
	private bool stopTouch;

	public float swipeRange;

	private void Start()
	{
		changeState(gameState.menu);
		sizeSlider.onValueChanged.AddListener(delegate { sliderChange(); });
		sizeSlider.minValue = 3;
		sizeSlider.maxValue = 20;
		sizeSlider.value = 4;

	}

	private void sliderChange()
	{
		width = Mathf.RoundToInt(sizeSlider.value);
		height = Mathf.RoundToInt(sizeSlider.value);

		sliderText.text = width + "x" + width;
	}

	private void destroyGame()
	{
		foreach (var cell in cells)
			Destroy(cell.gameObject);
		cells.Clear();

		foreach (var node in nodes)
			Destroy(node.gameObject);
		nodes.Clear();

		Destroy(board.gameObject);
	}

	public void restartGame()
	{
		destroyGame();

		changeState(gameState.generateLevel);
	}

	public void playGame()
	{
		changeState(gameState.generateLevel);
	}

	public void returnMenu()
	{
		destroyGame();
		scoreText.gameObject.SetActive(false);
		changeState(gameState.menu);
	}

	private void changeState(gameState newState)
	{
		state = newState;

		switch (newState)
		{
			case gameState.menu:
				menuScreen.SetActive(true);
				break;
			case gameState.generateLevel:
				generateGrid();
				break;
			case gameState.spawningCells:
				spawnCells(round++ == 0 ? 2 : 1);
				break;
			case gameState.waitingInput:
				break;
			case gameState.moving:
				break;
			case gameState.win:
				winScreen.SetActive(true);
				break;
			case gameState.lose:
				loseScreen.SetActive(true);

				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
		}
	}

	private void Update()
	{
		if (state != gameState.waitingInput)
			return;

		swipe();

		if (Input.GetKeyDown(KeyCode.LeftArrow))
			shift(Vector2.left);
		if (Input.GetKeyDown(KeyCode.RightArrow))
			shift(Vector2.right);
		if (Input.GetKeyDown(KeyCode.UpArrow))
			shift(Vector2.up);
		if (Input.GetKeyDown(KeyCode.DownArrow))
			shift(Vector2.down);
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			returnMenu();
		}

	}

	public void swipe()
	{
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
		{
			startTouchPos = Input.GetTouch(0).position;
		}
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
		{
			currentTouchPos = Input.GetTouch(0).position;
			Vector2 distance = currentTouchPos - startTouchPos;

			if (!stopTouch)
			{
				if (distance.x < -swipeRange)
				{
					shift(Vector2.left);
					stopTouch = true;
				}
				else if (distance.x > swipeRange)
				{
					shift(Vector2.right);
					stopTouch = true;
				}
				else if (distance.y > swipeRange)
				{
					shift(Vector2.up);
					stopTouch = true;
				}
				else if (distance.x < -swipeRange)
				{
					shift(Vector2.down);
					stopTouch = true;
				}
			}
		}
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
		{
			stopTouch = false;
			endTouchPosition = Input.GetTouch(0).position;
		}
	}

	void generateGrid()
	{
		Camera.main.orthographicSize = width;
		score = 0;
		scoreText.text = score.ToString();
		scoreText.gameObject.SetActive(true);

		round = 0;
		nodes = new List<Node>();
		cells = new List<Cell>();

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				var node = Instantiate(nodePrefab, new Vector2(x, y), Quaternion.identity);
				nodes.Add(node);
			}
		}

		var center = new Vector2((float)width / 2 - 0.5f, (float)height / 2 - 0.5f);
		board = Instantiate(boardPrefab, center, Quaternion.identity);
		board.size = new Vector2(width, height);

		Camera.main.transform.position = new Vector3(center.x, center.y, -10);

		changeState(gameState.spawningCells);
	}

	void spawnCells(int amount)
	{
		var availableNodes = nodes.Where(n => n.occupiedCell == null).OrderBy(b => UnityEngine.Random.value);

		foreach (var node in availableNodes.Take(amount))
		{
			spawnCell(node, UnityEngine.Random.value > 0.8f ? 4 : 2);
		}

		if (availableNodes.Count() == 0)
		{
			changeState(gameState.lose);
			return;
		}

		changeState(cells.Any(b => b.value == winCondition) ? gameState.win : gameState.waitingInput);
	}

	void spawnCell(Node node, int value)
	{
		var cell = Instantiate(cellPrefab, node.pos, Quaternion.identity);
		cell.init(getCellTypeByValue(value));
		cell.setCell(node);
		cells.Add(cell);
	}

	void shift(Vector2 dir)
	{
		changeState(gameState.moving);
		var orderCells = cells.OrderBy(b => b.pos.x).ThenBy(b => b.pos.y).ToList();
		if (dir == Vector2.right || dir == Vector2.up)
			orderCells.Reverse();

		foreach (var cell in orderCells)
		{
			var next = cell.Node;
			do {
				cell.setCell(next);
				var possibleNode = getNodeAtPos(next.pos + dir);
				if (possibleNode != null)
				{
					if (possibleNode.occupiedCell != null && possibleNode.occupiedCell.canMerge(cell.value))
						cell.mergeBothCell(possibleNode.occupiedCell);

					else if (possibleNode.occupiedCell == null)
						next = possibleNode;
				}

			} while (next != cell.Node);

		}

		var sequence = DOTween.Sequence();

		foreach (var cell in orderCells)
		{
			var movePoint = cell.mergeCell != null ? cell.mergeCell.Node.pos : cell.Node.pos;

			sequence.Insert(0, cell.transform.DOMove(movePoint, travelTime));
		}

		sequence.OnComplete(() =>
		{
			foreach (var cell in orderCells.Where(b => b.mergeCell != null))
			{
				mergeCells(cell.mergeCell, cell);
			}
			changeState(gameState.spawningCells);
		});
	}

	void mergeCells(Cell baseCell, Cell mergingCell)
	{
		score += baseCell.value * 2;
		scoreText.text = score.ToString();
		spawnCell(baseCell.Node, baseCell.value * 2);
		removeCell(baseCell);
		removeCell(mergingCell);
	}

	void removeCell(Cell cell)
	{
		cells.Remove(cell);
		Destroy(cell.gameObject);
	}

	Node getNodeAtPos(Vector2 pos)
	{
		return nodes.FirstOrDefault(n => n.pos == pos);
	}

	public void doExitGame()
	{
		Application.Quit();
	}
}

[Serializable]
public struct cellType
{
	public int value;
	public Color color;
}

public enum gameState
{
	menu,
	generateLevel,
	spawningCells,
	waitingInput,
	moving,
	win,
	lose
}