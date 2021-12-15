using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.XPath;

public class SceneLoader : MonoBehaviour
{
	public GameObject originObject;
	public TextAsset calibrationData;
	public Material cameraMaterial;
	public Material cameraFrustumMaterial;
	public float maxCameraFrustumLength = 4.0f;

	private List<GameObject> cameraObjects = new List<GameObject>();

	private bool camerasLocked = false;
	private Vector3 positionOffset;
	private float yRotationOffset;

	private bool updateCameraFrustums = false;
	private float _cameraFrustumLength = 1.0f;
	public float cameraFrustumLength
	{
		get
		{
			return _cameraFrustumLength;
		}
		set
		{
			_cameraFrustumLength = value;
			updateCameraFrustums = true;
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		LoadCameraData();
	}

	// Update is called once per frame
	void Update()
	{
		if (updateCameraFrustums)
		{
			foreach (GameObject obj in cameraObjects)
				obj.GetComponent<ViconCamera>().GenerateCameraFrustumMesh(cameraFrustumLength);

			updateCameraFrustums = false;
		}
	}

	private void LateUpdate()
	{
		if (camerasLocked)
		{
			// Only copy rotation around y axis
			var eulerRot = Camera.current.transform.rotation.eulerAngles;

			originObject.transform.RotateAround(Camera.current.transform.position, Vector3.up, eulerRot.y - yRotationOffset);
			yRotationOffset = eulerRot.y;

			var pos = originObject.transform.position;
			pos = Camera.current.transform.position + positionOffset;
			originObject.transform.position = pos;
		}
	}

	void LoadCameraData()
	{
		// Destroy previous camera objects if there are any
		foreach (GameObject o in cameraObjects)
			Destroy(o);

		cameraObjects.Clear();

		// Parse xml file and generate a new camera object for each valid entry
		var doc = new XPathDocument(new System.IO.MemoryStream(calibrationData.bytes));

		foreach (XPathNavigator cam in doc.CreateNavigator().Select("/Cameras/Camera"))
		{
			// NOTE: The format supports multiple key frames for moving cameras but in our case there's only one
			var keyframe = cam.SelectSingleNode("./KeyFrames/KeyFrame");

			// Position

			var attrib = keyframe.GetAttribute("POSITION", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing POSITION attribute");
				continue;
			}

			var components = attrib.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

			if (components.Length != 3)
			{
				Debug.LogError("Expected 3 components for position, got " + components.Length);
				continue;
			}

			var pos = new Vector3
			(
				float.Parse(components[0]),
				float.Parse(components[2]), // y and z axes need to be swapped
				float.Parse(components[1])
			);

			pos /= 1000.0f; // Convert to unity's scale where 1 unit = 1 meter

			// Orientation

			attrib = keyframe.GetAttribute("ORIENTATION", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing ORIENTATION attribute");
				continue;
			}

			components = attrib.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

			if (components.Length != 4)
			{
				Debug.LogError("Expected 4 components for orientation, got " + components.Length);
				continue;
			}

			var rot = new Quaternion
			(
				float.Parse(components[0]),
				float.Parse(components[1]),
				float.Parse(components[2]),
				float.Parse(components[3])
			);

			// Swap y and z rotation by converting quaternion to euler and back
			var euler = rot.eulerAngles;
			var temp = euler.y;
			euler.x += 90.0f; // Add 90 degree rotation around x since Unity's cameras face forward if rotation is zero but we need it to face down for correct transformation
			euler.y = euler.z;
			euler.z = temp;
			rot = Quaternion.Euler(euler);

			// Sensor size

			attrib = cam.GetAttribute("SENSOR_SIZE", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing SENSOR_SIZE attribute");
				continue;
			}

			components = attrib.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

			if (components.Length != 2)
			{
				Debug.LogError("Expected 2 components for orientation, got " + components.Length);
				continue;
			}

			var sensorSize = new Vector2(float.Parse(components[0]), float.Parse(components[1]));

			// Focal length

			attrib = keyframe.GetAttribute("FOCAL_LENGTH", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing FOCAL_LENGTH attribute");
				continue;
			}

			var focalLength = float.Parse(attrib);

			// Setup game object

			var obj = ViconCamera.Create(cam.GetAttribute("NAME", "") + " (" + cam.GetAttribute("DEVICEID", "") + ")", pos, rot, cameraMaterial, cameraFrustumMaterial, cameraFrustumLength, focalLength, sensorSize);

			obj.transform.parent = originObject.transform;
			cameraObjects.Add(obj);
		}
	}

	public void OnSliderUpdate(Microsoft.MixedReality.Toolkit.UI.SliderEventData data)
	{
		cameraFrustumLength = data.NewValue * maxCameraFrustumLength;
	}

	public void OnButtonPressed()
	{
		camerasLocked = !camerasLocked;
		positionOffset = originObject.transform.position - Camera.current.transform.position;
		yRotationOffset = Camera.current.transform.eulerAngles.y;
	}
}
