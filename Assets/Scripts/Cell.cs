using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class Cell : MonoBehaviour
{
	public int value;
	public Node Node;
	public Cell mergeCell;
	public bool merging;

	[SerializeField] private SpriteRenderer renderer;
	[SerializeField] private TextMeshPro text;

	public Vector2 pos => transform.position;


	public void init(cellType type)
	{
		this.value = type.value;
		renderer.color = type.color;
		text.text = type.value.ToString();

	}

	public void setCell(Node node)
	{
		if (Node != null)
			Node.occupiedCell = null;
		Node = node;
		Node.occupiedCell = this;
	}

	public void mergeBothCell(Cell cellToMergeWith)
	{
		mergeCell = cellToMergeWith;
		 
		Node.occupiedCell = null;

		cellToMergeWith.merging = true;
	}

	public bool canMerge(int Value) => Value == value && !merging && mergeCell == null;
}
