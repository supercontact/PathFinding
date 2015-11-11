using UnityEngine;
using System.Collections;

public class ObserveCamera : MonoBehaviour {

	public GameObject target;
	public float mouseControlDistance = 500;
	public float mouseScrollFactor = 0.1f;
	public float smooth = 10;

	private float targetDistance;
	private float distance;
	private Quaternion targetRotation;
	

	private Vector3 prevMousePos;


	void Start () {
		targetDistance = (target.transform.position - transform.position).magnitude;
		targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
		transform.rotation = targetRotation;
	}
	
	// Update is called once per frame
	void Update () {
		if (Time.deltaTime != 0f) {
			if (Input.GetMouseButtonDown(0)) {
				prevMousePos = Input.mousePosition;
			} else if (Input.GetMouseButton(0)) {
				Vector3 v1 = new Vector3(prevMousePos.x - Screen.width / 2, prevMousePos.y - Screen.height / 2, -mouseControlDistance);
				Vector3 v2 = new Vector3(Input.mousePosition.x - Screen.width / 2, Input.mousePosition.y - Screen.height / 2, -mouseControlDistance);
				Quaternion rot = Quaternion.Inverse(Quaternion.FromToRotation(v1, v2));
				targetRotation = transform.rotation * rot * Quaternion.Inverse(transform.rotation) * targetRotation;
				prevMousePos = Input.mousePosition;
			}
			targetDistance *= Mathf.Exp(- Input.mouseScrollDelta.y * mouseScrollFactor);

			transform.rotation = Quaternion.Slerp(targetRotation, transform.rotation, Mathf.Exp(- smooth * Time.deltaTime));
			distance = Mathf.Lerp(targetDistance, distance, Mathf.Exp(- smooth * Time.deltaTime));
			transform.position = target.transform.position + distance * (transform.rotation * Vector3.back);
		}
	}


}
